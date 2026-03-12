import { describe, it, expect, vi, beforeEach } from 'vitest';
import { SpSignalrService } from './spulse-signalr.service';

// --- Mock HubConnection functions ---
const mockHubStart = vi.fn(async () => { });
const mockHubStop = vi.fn(async () => { });
const mockHubOn = vi.fn();
const mockHubOnReconnected = vi.fn();

// Mock SignalR classes
interface MockHubBuilder {
  withUrl(url: string, options?: any): this;
  withAutomaticReconnect(): this;
  configureLogging(level: any): this;
  build(): any;
}

// Mock HubConnectionBuilder constructor
function MockHubConnectionBuilder(this: MockHubBuilder) {
  // nothing to return; methods are on prototype
}

MockHubConnectionBuilder.prototype.withUrl = function (url: string, options?: any) {
  return this;
};
MockHubConnectionBuilder.prototype.withAutomaticReconnect = function () {
  return this;
};
MockHubConnectionBuilder.prototype.configureLogging = function (level: any) {
  return this;
};
MockHubConnectionBuilder.prototype.build = function () {
  return {
    start: mockHubStart,
    stop: mockHubStop,
    on: mockHubOn,
    onreconnected: mockHubOnReconnected,
  };
};

// Mock the SignalR module
vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: MockHubConnectionBuilder,
  LogLevel: { Information: 1 },
}));

// Tests
describe('SpSignalrService', () => {
  let service: SpSignalrService;
  let emitted: any[];

  beforeEach(() => {
    service = new SpSignalrService();
    emitted = [];

    // Capture emitted values
    const originalSet = service['genEvent'].set;
    service['genEvent'].set = (val: any) => {
      if (val) emitted.push(val);
      return originalSet.call(service['genEvent'], val);
    };

    // Reset mocks
    mockHubStart.mockClear();
    mockHubStop.mockClear();
    mockHubOn.mockClear();
    mockHubOnReconnected.mockClear();
  });

  it('should start SignalR with URL and optional token', async () => {

    const hubUrl = 'http://fakehub';
    const token = '123token';

    await service.start(hubUrl, token);

    expect(mockHubStart).toHaveBeenCalled();
    expect(mockHubOn).toHaveBeenCalledTimes(service['eventsToSubscribe'].length);
    expect(mockHubOnReconnected).toHaveBeenCalledTimes(1);
  });

  it('should emit events in order and dedupe by eventId', () => {

    const evt1 = { eventId: '1', type: 'quote.created', sequence: 1, timestamp: '', payload: {} };
    const evt2 = { ...evt1 }; // duplicate
    const evt3 = { ...evt1, eventId: '2', sequence: 2 };

    service['handleEvent'](evt1 as any);
    service['handleEvent'](evt2 as any); // should be deduped
    service['handleEvent'](evt3 as any);

    expect(emitted.length).toBe(2);
    expect(emitted[0].eventId).toBe('1');
    expect(emitted[1].eventId).toBe('2');
  });

  it('should ignore out-of-order or old sequence events', () => {

    service['lastSequence'] = 1;

    const oldEvt = { eventId: 'old', type: 'quote.created', sequence: 0, timestamp: '', payload: {} };
    const newEvt = { eventId: 'new', type: 'quote.created', sequence: 2, timestamp: '', payload: {} };

    service['handleEvent'](oldEvt as any);
    service['handleEvent'](newEvt as any);

    expect(emitted.length).toBe(1);
    expect(emitted[0].eventId).toBe('new');
  });

  it('should reset state correctly', () => {

    service['lastSequence'] = 5;
    service['queue'].set(1, { eventId: 'x', type: 'quote.created', sequence: 1, timestamp: '', payload: {} });
    service['seen'].set('x', Date.now());

    service.resetState();

    expect(service['lastSequence']).toBe(0);
    expect(service['queue'].size).toBe(0);
    expect(service['seen'].size).toBe(0);
    expect(service.genEvent()).toBeNull();
  });

  it('should stop SignalR connection', async () => {

    await service.start('http://fakehub');
    await service.stop();

    expect(mockHubStop).toHaveBeenCalled();
  });
});