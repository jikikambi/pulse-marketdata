import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { QuotePayload } from './models/quote-payload.model';
import { EntityCollectionService, EntityCollectionServiceFactory } from '@ngrx/data';
import { QuoteService } from './services/quote.service';
import { QuoteDataService } from './services/quote-data.service';
import { QuoteAIInsightPayload } from './models/quote-ai-insights.model';
import { QuoteAiInsightService } from './services/quote-ai-insight.service';
import { QuoteAiInsightDataService } from './services/qoute-ai-insight-data.service';
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

  private readonly svcFactory = inject(EntityCollectionServiceFactory);

  private readonly quoteSvc: EntityCollectionService<QuotePayload> = this.svcFactory.create<QuotePayload>('Quote');
  private readonly aiInsightSvc: EntityCollectionService<QuoteAIInsightPayload> = this.svcFactory.create<QuoteAIInsightPayload>('QuoteInsight');

  private quoteHttpSvc = inject(QuoteService);
  private aiInsightHttpSvc = inject(QuoteAiInsightService);

  private quoteDataSvc = inject(QuoteDataService);
  private aiInsightDataSvc = inject(QuoteAiInsightDataService);

  constructor() {

    this.loadQuotes();

    this.loadAIInsights();
  }

  loadQuotes() {

    this.quoteHttpSvc.loadQuotes();

    this.quoteDataSvc.getAll().subscribe({

      next: quotes => {
       
        this.quoteSvc.addAllToCache([...quotes]);
      },

      error: err => console.log('[App] Failed to fetch quotes:', err)
    });
  }

  loadAIInsights() {

    this.aiInsightHttpSvc.loadAIInsights();

    this.aiInsightDataSvc.getAll().subscribe({

      next: insights => {
        
        this.aiInsightSvc.addAllToCache([...insights]);
      },

      error: err => console.log('[App] Failed to fetch AI insights:', err)
    });
  }
}
