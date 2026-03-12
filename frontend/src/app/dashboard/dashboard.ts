import { CommonModule } from '@angular/common';
import { Component, effect, inject, ViewEncapsulation } from '@angular/core';

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
export class Dashboard {

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