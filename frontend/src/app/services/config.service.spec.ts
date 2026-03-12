import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ConfigService } from './config.service';

describe('ConfigService', () => {

    let service: ConfigService;
    let httpMock: HttpTestingController;

    const mockConfig = {
        signalRHubUrl: 'http://localhost:7296/hub'
    };

    beforeEach(() => {

        TestBed.configureTestingModule({
            providers: [ConfigService, provideHttpClientTesting()]
        });

        service = TestBed.inject(ConfigService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should load config and set ready signal', async () => {

        const promise = service.load();

        const req = httpMock.expectOne('/config');
        expect(req.request.method).toBe('GET');

        req.flush(mockConfig);

        await promise;

        expect(service.ready()).toBe(true);
        expect(service.config).toEqual(mockConfig);
    });

    it('should throw if config accessed before load', () => {

        expect(() => service.config).toThrowError('Config not loaded yet!');
    });

    it('should not reload config if already loaded', async () => {

        const firstLoad = service.load();

        const req = httpMock.expectOne('/config');
        req.flush(mockConfig);

        await firstLoad;

        await service.load(); // second call

        httpMock.expectNone('/config'); // no second request
    });

    it('should expose correct config after load', async () => {

        const promise = service.load();

        const req = httpMock.expectOne('/config');
        req.flush(mockConfig);

        await promise;

        expect(service.config.signalRHubUrl).toBe('http://localhost:7296/hub');
    });
});