import { EntityDataModuleConfig, EntityMetadataMap } from "@ngrx/data";
import { QuoteCreatedPayload } from "./models/quote-created.model";
import { CustomDataServiceConfig } from "./services/provide-custom-data-services";
import { QuoteDataService } from "./services/quote-data.service";

export const entityMetadata: EntityMetadataMap = {

    Quote: { selectId: (q: QuoteCreatedPayload) => q.id }
}

export const entityConfig: EntityDataModuleConfig = {

    entityMetadata
}

export const customeDataServices: CustomDataServiceConfig[] = [

    { entityName: 'Quote', dataService: QuoteDataService }
];