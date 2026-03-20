import { EntityDataModuleConfig, EntityMetadataMap } from "@ngrx/data";
import { QuotePayload } from "./models/quote-payload.model";
import { CustomDataServiceConfig } from "./services/provide-custom-data-services";
import { QuoteDataService } from "./services/quote-data.service";
import { QuoteAIInsightPayload } from "./models/quote-ai-insights.model";
import { QuoteAiInsightDataService } from "./services/qoute-ai-insight-data.service";
import { ForexAIInsightPayload } from "./models/forex-ai-insights.model";
import { ForexAiInsightDataService } from "./services/forex-ai-insight-data.service";
import { ForexPayload } from "./models/forex-payload.model";
import { ForexDataService } from "./services/forex-data.service";

export const entityMetadata: EntityMetadataMap = {

    Quote: { selectId: (q: QuotePayload) => q.id },
    Forex: { selectId: (f: ForexPayload) => f.id },
    QuoteInsight: { selectId: (ai: QuoteAIInsightPayload) => ai.id },
    ForexInsight: { selectId: (ai: ForexAIInsightPayload) => ai.id }
}

export const entityConfig: EntityDataModuleConfig = {

    entityMetadata
}

export const customeDataServices: CustomDataServiceConfig[] = [

    { entityName: 'Quote', dataService: QuoteDataService },
    { entityName: 'Forex', dataService: ForexDataService },
    { entityName: 'QuoteInsight', dataService: QuoteAiInsightDataService },
    { entityName: 'ForexInsight', dataService: ForexAiInsightDataService }
];