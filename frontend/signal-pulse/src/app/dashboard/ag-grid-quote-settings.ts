import { ColDef } from "ag-grid-community";
import { QuotePayload } from "../models/quote-payload.model";

export class AgGridQuoteSettings {

    columnDefs: ColDef<QuotePayload>[] = [];

    constructor() {

        this.columnDefs = [
            
            { field: 'symbol', sortable: true, resizable: false },
            { field: 'price', sortable: true, resizable: true },
            { field: 'changePercent', headerName: '% Change' }
        ];
    }
}