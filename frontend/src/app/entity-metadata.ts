import { EntityDataModuleConfig, EntityMetadataMap } from "@ngrx/data";
import { QuotePayload } from "./models/quote-payload.model";
import { CustomDataServiceConfig } from "./services/provide-custom-data-services";
import { QuoteDataService } from "./services/quote-data.service";
import { QuoteAIInsightPayload } from "./models/quote-ai-insights.model";
import { AIInsightDataService } from "./services/ai-insight-data.service";

export const entityMetadata: EntityMetadataMap = {

    Quote: { selectId: (q: QuotePayload) => q.id },
    AIInsight: { selectId: (ai: QuoteAIInsightPayload) => ai.id }
}

export const entityConfig: EntityDataModuleConfig = {

    entityMetadata
}

export const customeDataServices: CustomDataServiceConfig[] = [

    { entityName: 'Quote', dataService: QuoteDataService },
    { entityName: 'AIInsight', dataService: AIInsightDataService }
];