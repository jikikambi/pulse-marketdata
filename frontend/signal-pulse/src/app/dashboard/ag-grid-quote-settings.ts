import { ColDef } from "ag-grid-community";
import { QuoteCreatedPayload } from "../models/quote-created.model";

export class AgGridQuoteSettings {

    columnDefs: ColDef<QuoteCreatedPayload>[] = [];

    constructor() {

        this.columnDefs = [
            
            { field: 'symbol', sortable: true, resizable: false },
            { field: 'price', sortable: true, resizable: true },
            { field: 'changePercent', headerName: '% Change' }
        ];
    }
}