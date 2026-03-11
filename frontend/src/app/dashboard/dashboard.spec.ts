import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { Component, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { SpSignalrSyncService } from '../services/spulse-signalr-sync.service';
import { EntityCollectionServiceFactory } from '@ngrx/data';

// MOCK SpSignalrSyncService
class MockSpSignalrSyncService {
  quoteSig = signal<any[]>([]);
  aiInsightSig = signal<any[]>([]);
  connect = vi.fn();
  disconnect = vi.fn();
}

// MOCK AG Grid Component
@Component({
  selector: 'ag-grid-angular',
  template: '<div></div>',
})
class MockAgGridAngular {}

// TEST SUITE
describe('Dashboard', () => {
  let component: any;
  let fixture: ComponentFixture<any>;

  beforeEach(async () => {
    const mod = await import('./dashboard');
    const DashboardComponent = mod.Dashboard;

    await TestBed.configureTestingModule({
      imports: [
        DashboardComponent,
        MatCardModule,
        MatDividerModule,
        MockAgGridAngular
      ],
      providers: [
        { provide: SpSignalrSyncService, useClass: MockSpSignalrSyncService },
        { provide: EntityCollectionServiceFactory, useValue: {} } // Mock just to satisfy injection
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});