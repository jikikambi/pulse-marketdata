import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { QuoteCreatedPayload } from './models/quote-created.model';
import { EntityCollectionService, EntityCollectionServiceFactory } from '@ngrx/data';
import { QuoteService } from './services/quote.service';
import { QuoteDataService } from './services/quote-data.service';
import { toSignal } from '@angular/core/rxjs-interop';
import { AIInsightPayload } from './models/ai-insights.model';
import { AIInsightService } from './services/aiinsight.service';
import { AIInsightDataService } from './services/aiinsight-data.service';
import { MatCardModule } from '@angular/material/card';
import { Dashboard } from './dashboard/dashboard';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, MatToolbarModule, MatCardModule, Dashboard],
  templateUrl: './app.html',
  styleUrls: ['./app.scss']
})
export class App {

  protected readonly title = signal('Signal Pulse');

  readonly quotes = signal<QuoteCreatedPayload[]>([]);
  readonly aiInsight = signal<AIInsightPayload[]>([]);

  private readonly svcFactory = inject(EntityCollectionServiceFactory);

  private readonly quoteSvc: EntityCollectionService<QuoteCreatedPayload> = this.svcFactory.create<QuoteCreatedPayload>('Quote');
  private readonly aiInsightSvc: EntityCollectionService<AIInsightPayload> = this.svcFactory.create<AIInsightPayload>('AIInsight');

  private quoteHttpSvc = inject(QuoteService);
  private aiInsightHttpSvc = inject(AIInsightService);

  private quoteDataSvc = inject(QuoteDataService);
  private aiInsightDataSvc = inject(AIInsightDataService);

  constructor() {

    // 1️. Initialize NgRx data -> signal binding 
    const quotes$ = this.quoteSvc.entities$;
    const allQuotesSignal = toSignal(quotes$, { initialValue: [] as QuoteCreatedPayload[] });

    const aiInsights$ = this.aiInsightSvc.entities$;
    const allAIInsightSignal = toSignal(aiInsights$, { initialValue: [] as AIInsightPayload[] });

    // 2. Keep NgRx cache and local signal in sync
    effect(() => this.quotes.set(allQuotesSignal()));

    effect(() => this.aiInsight.set(allAIInsightSignal()));

    this.loadQuotes();

    this.loadAIInsights();
  }

  loadQuotes() {

    this.quoteHttpSvc.loadQuotes();

    this.quoteDataSvc.getAll().subscribe({

      next: quotes => {

        console.log(quotes)
        this.quoteSvc.addAllToCache(quotes);
        this.quotes.set(quotes);
      },

      error: err => console.log('[App] Failed to fetch quotes:', err)
    });
  }

  loadAIInsights() {

    this.aiInsightHttpSvc.loadAIInsights();

    this.aiInsightDataSvc.getAll().subscribe({

      next: insights => {

        console.log(insights)
        this.aiInsightSvc.addAllToCache(insights);
        this.aiInsight.set(insights);
      },

      error: err => console.log('[App] Failed to fetch ai-insights:', err)
    });
  }
}
