// Disk entity types (UDA files), unless the Deploy edition is BackOfficeOnly, which makes them transfer entity types that can be queued and exported.
// Deploy's conditions hide the actions otherwise, and it strips "-root" before mapping, so the roots resolve through the same mapping.
const entityTypes = {
  "uai:connection": "umbraco-ai-connection",
  "uai:context": "umbraco-ai-context",
  "uai:guardrail": "umbraco-ai-guardrail",
  "uai:profile": "umbraco-ai-profile",
  "uai:settings": "umbraco-ai-settings",
};

const actionEntityTypes = [
  "uai:connection-root",
  "uai:connection",
  "uai:context-root",
  "uai:context",
  "uai:guardrail-root",
  "uai:guardrail",
  "uai:profile-root",
  "uai:profile",
  "uai:settings-root",
];

export const onInit = async (host, extensionRegistry) => {
  extensionRegistry.registerMany([
    {
      type: "localization",
      alias: "Umbraco.AI.Deploy.Localization.En",
      name: "Umbraco AI Deploy English",
      weight: -100,
      meta: {
        culture: "en",
        localizations: {
          deploy_entityTypes: {
            "umbraco-ai-connection": "AI connection",
            "umbraco-ai-context": "AI context",
            "umbraco-ai-guardrail": "AI guardrail",
            "umbraco-ai-profile": "AI profile",
            "umbraco-ai-settings": "AI settings",
          },
        },
      },
    },
    {
      type: "deployEntityTypeMapping",
      alias: "Umbraco.AI.Deploy.EntityTypeMapping",
      name: "Umbraco AI Deploy Entity Type Mapping",
      entityTypes,
    },
    {
      // Shown when the entity type supports queue for transfer
      type: "deployEntityActionRegistrar",
      actionAlias: "Deploy.EntityAction.Queue",
      alias: "Umbraco.AI.Deploy.Queue.Registrar",
      name: "Umbraco AI Deploy Queue Entity Action Registrar",
      forEntityTypes: actionEntityTypes,
    },
    {
      // Shown when the entity type supports import and export
      type: "deployEntityActionRegistrar",
      actionAlias: "Deploy.EntityAction.Export",
      alias: "Umbraco.AI.Deploy.Export.Registrar",
      name: "Umbraco AI Deploy Export Entity Action Registrar",
      forEntityTypes: actionEntityTypes,
    },
  ]);
};
