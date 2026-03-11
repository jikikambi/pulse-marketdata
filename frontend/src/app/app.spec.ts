import { TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';
import { EntityCollectionServiceFactory, EntityCollectionService } from '@ngrx/data';
import { QuoteService } from './services/quote.service';
import { QuoteDataService } from './services/quote-data.service';
import { AIInsightService } from './services/aiinsight.service';
import { AIInsightDataService } from './services/aiinsight-data.service';
import { SpSignalrSyncService } from './services/spulse-signalr-sync.service';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatCardModule } from '@angular/material/card';


// MOCK Dashboard Component
@Component({
  selector: 'app-dashboard',
  standalone: true,
  template: '<div></div>',
})
class MockDashboard {}

// MOCK SERVICES
class MockEntityCollectionServiceFactory {
  create<T>(name: string): EntityCollectionService<T> {
    return {
      addAllToCache: vi.fn(),
    } as unknown as EntityCollectionService<T>;
  }
}

class MockQuoteService { loadQuotes = vi.fn(); }
class MockAIInsightService { loadAIInsights = vi.fn(); }
class MockQuoteDataService { getAll = vi.fn(() => of([])); }
class MockAIInsightDataService { getAll = vi.fn(() => of([])); }

// MOCK SpSignalrSyncService
class MockSpSignalrSyncService {
  quoteSig = signal<any[]>([]);       // <--- must be callable
  aiInsightSig = signal<any[]>([]);   // <--- must be callable
  connect = vi.fn();
  disconnect = vi.fn();
}

// TEST SUITE
describe('App', () => {
  let AppComponent: any;

  beforeEach(async () => {
    const mod = await import('./app');
    AppComponent = mod.App;

    await TestBed.configureTestingModule({
      imports: [
        AppComponent,
        MockDashboard,
        MatToolbarModule,
        MatCardModule, 
      ],
      providers: [
        { provide: EntityCollectionServiceFactory, useClass: MockEntityCollectionServiceFactory },
        { provide: QuoteService, useClass: MockQuoteService },
        { provide: AIInsightService, useClass: MockAIInsightService },
        { provide: QuoteDataService, useClass: MockQuoteDataService },
        { provide: AIInsightDataService, useClass: MockAIInsightDataService },
        { provide: SpSignalrSyncService, useClass: MockSpSignalrSyncService },
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render title', async () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('span')?.textContent).toContain('Signal Pulse');
  });
});