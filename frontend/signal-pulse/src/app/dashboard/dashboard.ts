import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, OnInit, SimpleChanges, ViewEncapsulation } from '@angular/core';

import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { AgGridAngular } from 'ag-grid-angular';
import { AIInsightPayload } from '../models/ai-insights.model';
import { QuoteCreatedPayload } from '../models/quote-created.model';
import { AllCommunityModule, GridOptions, ModuleRegistry } from 'ag-grid-community';
import { AgGridQuoteSettings } from './ag-grid-quote-settings';
import { SentimentSummary } from '../models/sentiment-summary.model';

// Register AG Grid modules ONCE (outside component)
ModuleRegistry.registerModules([AllCommunityModule]);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatDividerModule, AgGridAngular],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.scss'],
  encapsulation: ViewEncapsulation.None
})
export class Dashboard implements OnInit, OnChanges {

  @Input() quotes: QuoteCreatedPayload[] = [];
  @Input() insights: AIInsightPayload[] = [];

  gridOptions!: GridOptions;
  frameworkComponents: any;
  agGridQuoteSettings!: AgGridQuoteSettings;
  themeClass: string = 'ag-theme-material';

  // Derived Dashboard Data
  hotStocks: any[] = [];
  winners: any[] = [];
  losers: any[] = [];
  insightFeed: any[] = [];

  sentimentSummary: SentimentSummary = {
  bullish: 0,
  neutral: 0,
  bearish: 0
};

  ngOnInit(): void {

    this.setUpGrid();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['quotes'] || changes['insights']) {
      this.buildDashboardData();
    }
  }

  setUpGrid() {

    this.agGridQuoteSettings = new AgGridQuoteSettings();

    this.gridOptions = {

      columnDefs: this.agGridQuoteSettings.columnDefs,
      pagination: true,
      animateRows: true,
      defaultColDef: { resizable: true, sortable: true, filter: true },
      context: { componentParent: this }
    };

    this.frameworkComponents = {};
  }

  buildDashboardData() {

    const merged = this.quotes.map(q => ({
      ...q,
      insight: this.insights.find(i => i.symbol === q.symbol) || null,
      sentimentScore: this.getSentimentScore(q.symbol),
      hotScore: this.computeHotScore(q.symbol)
    }));

    this.hotStocks = [...merged].sort((a, b) => b.hotScore - a.hotScore)
      .slice(0, 10);

    this.winners = [...merged].filter(x => x.changePercent > 0)
      .sort((a, b) => b.changePercent - a.changePercent)
      .slice(0, 5);

    this.losers = [...merged].filter(x => x.changePercent < 0)
      .sort((a, b) => b.changePercent - a.changePercent)
      .slice(0, 5);

    this.sentimentSummary = {
      bullish: this.insights.filter(i => i.sentiment === 'bullish').length,
      neutral: this.insights.filter(i => i.sentiment === 'neutral').length,
      bearish: this.insights.filter(i => i.sentiment === 'bearish').length
    };  

    this.insightFeed = [... this.insights]
      .sort((a, b) => new Date(b.observedAt)
        .getTime() - new Date(a.observedAt)
          .getTime());
  }

  getSentimentScore(symbol: string): number {

    const insight = this.insights.find(i => i.symbol === symbol);

    if (!insight) return 0;

    switch (insight.sentiment.toLocaleLowerCase()) {
      case 'bullish': return 2;
      case 'bearish': return -2;
      default: return 0;
    }
  }

  computeHotScore(symbol: string) {

    const q = this.quotes.find(x => x.symbol === symbol);

    const insight = this.insights.find(i => i.symbol === symbol);

    if (!q) return 0;

    const momentum = q.changePercent;                    // e.g. -0.0083 → -0.83%  

    const sentiment = this.getSentimentScore(symbol);   // bullish/bearish
     
    const volatility = insight?.volatility === 'high' ? 1 : 0;

    const directionBonus =
      insight?.direction === 'up' ? 1 :
        insight?.direction === 'down' ? -1 : 0;

    return (
      momentum * 2 +
      sentiment * 3 +
      volatility * 2 +
      directionBonus
    );
  }
}
