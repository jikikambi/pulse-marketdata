import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { Component, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { SpSignalrSyncService } from '../services/spulse-signalr-sync.service';
import { EntityCollectionServiceFactory } from '@ngrx/data';
import { Dashboard } from './dashboard';

const mockQuotes = [
  { id: 1, symbol: 'AAPL', changePercent: 5 },
  { id: 2, symbol: 'TSLA', changePercent: -3 },
  { id: 3, symbol: 'NVDA', changePercent: 10 },
  { id: 4, symbol: 'MSFT', changePercent: 2 }
];

const mockInsights = [
  {
    id: 1,
    symbol: 'AAPL',
    sentiment: 'bullish',
    volatility: 'high',
    direction: 'up',
    rationale: 'Strong earnings',
    observedAt: '2025-01-01T10:00:00Z'
  },
  {
    id: 2,
    symbol: 'TSLA',
    sentiment: 'bearish',
    volatility: 'high',
    direction: 'down',
    rationale: 'Weak demand',
    observedAt: '2025-01-02T10:00:00Z'
  }
];

// MOCK SpSignalrSyncService
class MockSpSignalrSyncService {

  quoteSig = signal<any[]>([]);
  aiInsightSig = signal<any[]>([]);
}

// MOCK AG Grid Component
@Component({
  selector: 'ag-grid-angular',
  template: '<div></div>',
})
class MockAgGridAngular { }

// TEST SUITE
describe('Dashboard', () => {

  let component: Dashboard;
  let fixture: ComponentFixture<Dashboard>;
  let syncService: MockSpSignalrSyncService;

  beforeEach(async () => {

    await TestBed.configureTestingModule({
      imports: [
        Dashboard,
        MatCardModule,
        MatDividerModule,
        MockAgGridAngular
      ],
      providers: [
        { provide: SpSignalrSyncService, useClass: MockSpSignalrSyncService },
        { provide: EntityCollectionServiceFactory, useValue: {} } // Mock just to satisfy injection
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Dashboard);
    component = fixture.componentInstance;

    syncService = TestBed.inject(SpSignalrSyncService) as any;

    //await fixture.whenStable();
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should compute hotStocks correctly', () => {

    syncService.quoteSig.set(mockQuotes);
    syncService.aiInsightSig.set(mockInsights);

    fixture.detectChanges();

    const hotStocks = component.hotStocks();

    expect(hotStocks.length).toBeGreaterThan(0);

    // NVDA should be highest because 10% change
    expect(hotStocks[0].symbol).toBe('NVDA');
  });

  it('should return winners sorted by changePercent', () => {

    syncService.quoteSig.set(mockQuotes);
    syncService.aiInsightSig.set(mockInsights);

    fixture.detectChanges();

    const winners = component.winners();

    expect(winners.every(w => w.changePercent > 0)).toBe(true);

    expect(winners[0].symbol).toBe('NVDA');
  });

  it('should return losers sorted by negative changePercent', () => {

    syncService.quoteSig.set(mockQuotes);
    syncService.aiInsightSig.set(mockInsights);

    fixture.detectChanges();

    const losers = component.losers();

    expect(losers.length).toBe(1);
    expect(losers[0].symbol).toBe('TSLA');
  });

  it('should sort insightFeed by newest observedAt', () => {

    syncService.quoteSig.set(mockQuotes);
    syncService.aiInsightSig.set(mockInsights);

    fixture.detectChanges();

    const feed = component.insightFeed();

    expect(feed[0].symbol).toBe('TSLA'); // newer date
  });

  it('should compute sentiment summary', () => {

    syncService.quoteSig.set(mockQuotes);
    syncService.aiInsightSig.set(mockInsights);

    fixture.detectChanges();

    const summary = component.sentimentSummary();

    expect(summary.bullish).toBe(1);
    expect(summary.bearish).toBe(1);
    expect(summary.neutral).toBe(0);
  });

  it('should render winners in template', () => {

    syncService.quoteSig.set(mockQuotes);
    syncService.aiInsightSig.set(mockInsights);

    fixture.detectChanges();

    const compiled = fixture.nativeElement;

    expect(compiled.textContent).toContain('NVDA');
  });

});