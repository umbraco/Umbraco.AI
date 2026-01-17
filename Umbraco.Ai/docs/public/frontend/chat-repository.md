---
description: >-
  Low-level repository for direct chat API access.
---

# Chat Repository

`UaiChatRepository` provides lower-level access to chat operations. Use this when you need more control than `UaiChatController` provides.

## Import

{% code title="Import" %}
```typescript
import { UaiChatRepository } from '@umbraco-ai/backoffice';
```
{% endcode %}

{% hint style="info" %}
For most use cases, prefer `UaiChatController` which provides a simpler API. Use `UaiChatRepository` when you need to customize request handling.
{% endhint %}

## Constructor

```typescript
new UaiChatRepository(host: UmbControllerHost)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `host` | `UmbControllerHost` | The controller host |

## Methods

### complete

Performs a chat completion.

{% code title="Signature" %}
```typescript
async complete(request: UaiChatRequest): Promise<{ data?: UaiChatResult; error?: unknown }>
```
{% endcode %}

| Parameter | Type | Description |
|-----------|------|-------------|
| `request` | `UaiChatRequest` | The chat request |

{% code title="Example" %}
```typescript
import { LitElement } from 'lit';
import { UaiChatRepository, UaiChatRequest } from '@umbraco-ai/backoffice';

class MyElement extends LitElement {
    #repository = new UaiChatRepository(this);

    async chat() {
        const request: UaiChatRequest = {
            profileIdOrAlias: 'content-assistant',
            messages: [
                { role: 'user', content: 'Hello' }
            ]
        };

        const { data, error } = await this.#repository.complete(request);

        if (data) {
            console.log(data.message.content);
        }
    }
}
```
{% endcode %}

### stream

Performs a streaming chat completion.

{% code title="Signature" %}
```typescript
stream(request: UaiChatRequest): AsyncGenerator<UaiChatStreamChunk>
```
{% endcode %}

| Parameter | Type | Description |
|-----------|------|-------------|
| `request` | `UaiChatRequest` | The chat request |

{% hint style="warning" %}
Streaming is currently under development.
{% endhint %}

## Request Type

{% code title="UaiChatRequest" %}
```typescript
interface UaiChatRequest {
    /** Profile ID or alias. Null uses default profile. */
    profileIdOrAlias?: string | null;
    /** The conversation messages. */
    messages: UaiChatMessage[];
    /** AbortSignal for cancellation. */
    signal?: AbortSignal;
}
```
{% endcode %}

## When to Use Repository vs Controller

| Use Case | Recommended |
|----------|-------------|
| Basic chat in a custom element | `UaiChatController` |
| Need full request object control | `UaiChatRepository` |
| Building a custom abstraction | `UaiChatRepository` |
| Multiple chat contexts in one element | `UaiChatRepository` |

## Example: Building a Custom Service

{% code title="chat.service.ts" %}
```typescript
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiChatRepository, UaiChatMessage, UaiChatResult } from '@umbraco-ai/backoffice';

/**
 * Custom service wrapping chat functionality with retry logic.
 */
export class MyChatService {
    #repository: UaiChatRepository;
    #maxRetries: number;

    constructor(host: UmbControllerHost, maxRetries = 3) {
        this.#repository = new UaiChatRepository(host);
        this.#maxRetries = maxRetries;
    }

    async chatWithRetry(
        messages: UaiChatMessage[],
        profileAlias: string
    ): Promise<UaiChatResult | null> {
        let lastError: unknown;

        for (let attempt = 1; attempt <= this.#maxRetries; attempt++) {
            const { data, error } = await this.#repository.complete({
                profileIdOrAlias: profileAlias,
                messages
            });

            if (data) {
                return data;
            }

            lastError = error;
            console.warn(`Attempt ${attempt} failed, retrying...`);

            // Wait before retry (exponential backoff)
            await new Promise(resolve =>
                setTimeout(resolve, Math.pow(2, attempt) * 1000)
            );
        }

        console.error('All retry attempts failed:', lastError);
        return null;
    }
}
```
{% endcode %}

{% code title="Using the Custom Service" %}
```typescript
import { LitElement } from 'lit';
import { MyChatService } from './chat.service.js';

class MyElement extends LitElement {
    #chatService = new MyChatService(this, 3);

    async askWithRetry(question: string) {
        const result = await this.#chatService.chatWithRetry(
            [{ role: 'user', content: question }],
            'content-assistant'
        );

        if (result) {
            console.log('Got response:', result.message.content);
        } else {
            console.log('Failed after all retries');
        }
    }
}
```
{% endcode %}
