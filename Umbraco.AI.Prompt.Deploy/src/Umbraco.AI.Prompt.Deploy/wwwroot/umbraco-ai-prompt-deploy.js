// Disk entity types (UDA files), unless the Deploy edition is BackOfficeOnly, which makes them transfer entity types that can be queued and exported.
// Deploy's conditions hide the actions otherwise, and it strips "-root" before mapping, so the roots resolve through the same mapping.
const entityTypes = {
  "uai:prompt": "umbraco-ai-prompt",
};

const actionEntityTypes = [
  "uai:prompt-root",
  "uai:prompt",
];

export const onInit = async (host, extensionRegistry) => {
  extensionRegistry.registerMany([
    {
      type: "localization",
      alias: "Umbraco.AI.Prompt.Deploy.Localization.En",
      name: "Umbraco AI Prompt Deploy English",
      weight: -100,
      meta: {
        culture: "en",
        localizations: {
          deploy_entityTypes: {
            "umbraco-ai-prompt": "AI prompt",
          },
        },
      },
    },
    {
      type: "deployEntityTypeMapping",
      alias: "Umbraco.AI.Prompt.Deploy.EntityTypeMapping",
      name: "Umbraco AI Prompt Deploy Entity Type Mapping",
      entityTypes,
    },
    {
      // Shown when the entity type supports queue for transfer
      type: "deployEntityActionRegistrar",
      actionAlias: "Deploy.EntityAction.Queue",
      alias: "Umbraco.AI.Prompt.Deploy.Queue.Registrar",
      name: "Umbraco AI Prompt Deploy Queue Entity Action Registrar",
      forEntityTypes: actionEntityTypes,
    },
    {
      // Shown when the entity type supports import and export
      type: "deployEntityActionRegistrar",
      actionAlias: "Deploy.EntityAction.Export",
      alias: "Umbraco.AI.Prompt.Deploy.Export.Registrar",
      name: "Umbraco AI Prompt Deploy Export Entity Action Registrar",
      forEntityTypes: actionEntityTypes,
    },
  ]);
};
