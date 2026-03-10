import { CommonModule } from '@angular/common';
import { Component, effect, inject, ViewEncapsulation } from '@angular/core';

import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { AgGridAngular } from 'ag-grid-angular';
import { AIInsightPayload } from '../models/ai-insights.model';
import { QuotePayload } from '../models/quote-payload.model';
import { AllCommunityModule, GridApi, GridOptions, GridReadyEvent, ModuleRegistry } from 'ag-grid-community';
import { AgGridQuoteSettings } from './ag-grid-quote-settings';
import { SpSignalrSyncService } from '../services/spulse-signalr-sync.service';

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
export class Dashboard {

  private readonly sync = inject(SpSignalrSyncService);

  readonly quotes = this.sync.quoteSig;

  readonly insights = this.sync.aiInsightSig;

  private gridApi!: GridApi<any>;

  gridOptions!: GridOptions<any>;

  frameworkComponents: any;
  agGridQuoteSettings!: AgGridQuoteSettings;
  themeClass = 'ag-theme-material';

  // Derived dashboard data getters
  get hotStocks() {
    return this.computeDashboardData().hotStocks;
  }

  get winners() {
    return this.computeDashboardData().winners;
  }

  get losers() {
    return this.computeDashboardData().losers;
  }

  get insightFeed() {
    return this.computeDashboardData().insightFeed;
  }

  get sentimentSummary() {
    return this.computeDashboardData().sentimentSummary;
  }

  constructor() {

    this.setUpGrid();

    effect(() => {

      const quotes = this.quotes();

      if (this.gridApi && quotes.length) {

        this.gridApi.setGridOption('rowData', this.quotes());
      }
    });
  }

  setUpGrid() {

    this.agGridQuoteSettings = new AgGridQuoteSettings();

    this.gridOptions = {
      columnDefs: this.agGridQuoteSettings.columnDefs,
      pagination: true,
      animateRows: true,
      defaultColDef: { resizable: true, sortable: true, filter: true },

      onGridReady: (event) => this.onGridReady(event),

      context: { componentParent: this },

      getRowId: params => params.data.id
    };

    this.frameworkComponents = {};
  }

  onGridReady(event: GridReadyEvent<any>) {

    this.gridApi = event.api;

    this.gridApi.sizeColumnsToFit();

    // Load initial grid data
    this.gridApi.setGridOption('rowData', this.quotes());
  }

  // Compute all derived dashboard data on-demand
  private computeDashboardData() {

    const quotes = this.quotes();

    const insights = this.insights();

    const merged = quotes.map(q => ({
      ...q,
      insight: insights.find(i => i.symbol === q.symbol) || null,
      sentimentScore: this.getSentimentScore(q.symbol, insights),
      hotScore: this.computeHotScore(q.symbol, quotes, insights)
    }));

    return {

      hotStocks: [...merged].sort((a, b) => b.hotScore - a.hotScore).slice(0, 10),

      winners: [...merged].filter(x => x.changePercent > 0)
        .sort((a, b) => b.changePercent - a.changePercent)
        .slice(0, 5),

      losers: [...merged].filter(x => x.changePercent < 0)
        .sort((a, b) => b.changePercent - a.changePercent)
        .slice(0, 5),

      insightFeed: [...insights].sort((a, b) => new Date(b.observedAt).getTime() - new Date(a.observedAt).getTime()),

      sentimentSummary: {
        bullish: insights.filter(i => i.sentiment === 'bullish').length,
        neutral: insights.filter(i => i.sentiment === 'neutral').length,
        bearish: insights.filter(i => i.sentiment === 'bearish').length
      }
    };
  }

  private getSentimentScore(symbol: string, insights: AIInsightPayload[]): number {

    const insight = insights.find(i => i.symbol === symbol);

    if (!insight) return 0;

    switch (insight.sentiment.toLowerCase()) {
      case 'bullish': return 2;
      case 'bearish': return -2;
      default: return 0;
    }
  }

  private computeHotScore(symbol: string, quotes: QuotePayload[], insights: AIInsightPayload[]): number {

    const q = quotes.find(x => x.symbol === symbol);

    const insight = insights.find(i => i.symbol === symbol);

    if (!q) return 0;

    const momentum = q.changePercent;

    const sentiment = this.getSentimentScore(symbol, insights);

    const volatility = insight?.volatility === 'high' ? 1 : 0;

    const directionBonus =
      insight?.direction === 'up' ? 1 :
        insight?.direction === 'down' ? -1 : 0;

    return momentum * 2 + sentiment * 3 + volatility * 2 + directionBonus;
  }
}