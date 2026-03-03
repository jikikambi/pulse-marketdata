import { EntityDataModuleConfig, EntityMetadataMap } from "@ngrx/data";
import { QuoteCreatedPayload } from "./models/quote-created.model";
import { CustomDataServiceConfig } from "./services/provide-custom-data-services";
import { QuoteDataService } from "./services/quote-data.service";
import { AIInsightPayload } from "./models/ai-insights.model";
import { AIInsightDataService } from "./services/aiinsight-data.service";

export const entityMetadata: EntityMetadataMap = {

    Quote: { selectId: (q: QuoteCreatedPayload) => q.id },
    AIInsight: { selectId: (ai: AIInsightPayload) => ai.id }
}

export const entityConfig: EntityDataModuleConfig = {

    entityMetadata
}

export const customeDataServices: CustomDataServiceConfig[] = [

    { entityName: 'Quote', dataService: QuoteDataService },
    { entityName: 'AIInsight', dataService: AIInsightDataService }
];