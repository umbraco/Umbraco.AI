// Disk entity types (UDA files), unless the Deploy edition is BackOfficeOnly, which makes them transfer entity types that can be queued and exported.
// Deploy's conditions hide the actions otherwise, and it strips "-root" before mapping, so the roots resolve through the same mapping.
const entityTypes = {
  "uai:agent": "umbraco-ai-agent",
};

const actionEntityTypes = [
  "uai:agent-root",
  "uai:agent",
];

export const onInit = async (host, extensionRegistry) => {
  extensionRegistry.registerMany([
    {
      type: "localization",
      alias: "Umbraco.AI.Agent.Deploy.Localization.En",
      name: "Umbraco AI Agent Deploy English",
      weight: -100,
      meta: {
        culture: "en",
        localizations: {
          deploy_entityTypes: {
            "umbraco-ai-agent": "AI agent",
          },
        },
      },
    },
    {
      type: "deployEntityTypeMapping",
      alias: "Umbraco.AI.Agent.Deploy.EntityTypeMapping",
      name: "Umbraco AI Agent Deploy Entity Type Mapping",
      entityTypes,
    },
    {
      // Shown when the entity type supports queue for transfer
      type: "deployEntityActionRegistrar",
      actionAlias: "Deploy.EntityAction.Queue",
      alias: "Umbraco.AI.Agent.Deploy.Queue.Registrar",
      name: "Umbraco AI Agent Deploy Queue Entity Action Registrar",
      forEntityTypes: actionEntityTypes,
    },
    {
      // Shown when the entity type supports import and export
      type: "deployEntityActionRegistrar",
      actionAlias: "Deploy.EntityAction.Export",
      alias: "Umbraco.AI.Agent.Deploy.Export.Registrar",
      name: "Umbraco AI Agent Deploy Export Entity Action Registrar",
      forEntityTypes: actionEntityTypes,
    },
  ]);
};
