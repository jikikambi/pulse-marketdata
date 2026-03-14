import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, effect, inject, OnDestroy, signal, ViewEncapsulation } from '@angular/core';

import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { AgGridAngular } from 'ag-grid-angular';
import { AllCommunityModule, GridApi, GridOptions, GridReadyEvent, ModuleRegistry } from 'ag-grid-community';
import { AgGridQuoteSettings } from './ag-grid-quote-settings';
import { DashboardStore } from './dashboard.store';

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
export class Dashboard implements AfterViewInit, OnDestroy {

  private readonly store = inject(DashboardStore);

  readonly quotes = this.store.quotes;
  readonly insights = this.store.insights;

  private gridApi!: GridApi<any>;
  gridOptions!: GridOptions<any>;
  frameworkComponents: any;
  agGridQuoteSettings!: AgGridQuoteSettings;
  themeClass = 'ag-theme-material';

  readonly hotStocks = this.store.hotStocks;
  readonly winners = this.store.winners;
  readonly losers = this.store.losers;
  readonly insightFeed = this.store.insightFeed;
  readonly sentimentSummary = this.store.sentimentSummary;

  sections = [
    { id: 'hot-stocks', label: 'Hot Stocks' },
    { id: 'winners-losers', label: 'Winners / Losers' },
    { id: 'sentiment', label: 'Market Sentiment' },
    { id: 'insights', label: 'AI Insights' },
    { id: 'quotes', label: 'Quotes Grid' }
  ];

  activeSection = signal('hot-stocks');

  scrollProgress = 0;

  constructor() {

    this.setUpGrid();

    effect(() => {

      const quotes = this.quotes();

      if (this.gridApi && quotes.length) {

        this.gridApi.setGridOption('rowData', this.quotes());
      }
    });
  }

  ngAfterViewInit() {

    if (typeof IntersectionObserver === 'undefined') {
      return;
    }

    const observer = new IntersectionObserver((entries) => {

      entries.forEach(entry => {

        if (entry.isIntersecting) {

          queueMicrotask(() => {

            this.activeSection.set(entry.target.id);
          });
        }
      });
    }, { threshold: 0.6 });

    this.sections.forEach(s => {

      const el = document.getElementById(s.id);

      if (el) observer.observe(el);
    });

    window.addEventListener('scroll', this.updateScrollProgress.bind(this));
  }

  ngOnDestroy() {
    window.removeEventListener('scroll', this.updateScrollProgress);
  }

  updateScrollProgress() {

    const scrollTop = window.scrollY;

    const height = document.documentElement.scrollHeight - document.documentElement.clientHeight;

    this.scrollProgress = (scrollTop / height) * 100;

  }

  scrollTo(id: string) {
    document.getElementById(id)?.scrollIntoView({
      behavior: 'smooth',
      block: 'start'
    });
  }

  setUpGrid() {

    this.agGridQuoteSettings = new AgGridQuoteSettings();

    this.gridOptions = {

      columnDefs: this.agGridQuoteSettings.columnDefs,

      pagination: true,

      animateRows: true,

      deltaSort: true,

      getRowId: params => params.data.id,

      defaultColDef: { resizable: true, sortable: true, filter: true },

      onGridReady: (event) => this.onGridReady(event),

      context: { componentParent: this }
    };

    this.frameworkComponents = {};
  }

  onGridReady(event: GridReadyEvent<any>) {

    this.gridApi = event.api;

    this.gridApi.sizeColumnsToFit();

    // Load initial grid data
    this.gridApi.setGridOption('rowData', this.quotes());
  }
}