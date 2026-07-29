import { describe, expect, it } from "vitest";
import { isRuleSupported, pruneDeclaredSettings, UAI_PROFILE_SETTING_KEYS } from "./declared-settings.js";
import {
    UAI_CHAT_SETTING_RULES,
    UAI_EMBEDDING_SETTING_RULES,
    UAI_IMAGE_GENERATION_SETTING_RULES,
    UAI_SPEECH_TO_TEXT_SETTING_RULES,
} from "./rules.js";
import type {
    UaiChatProfileSettings,
    UaiEmbeddingProfileSettings,
    UaiImageGenerationProfileSettings,
    UaiSpeechToTextProfileSettings,
} from "../../../../types.js";

/**
 * These rules decide two things at once: whether a field renders, and whether a stored value survives a
 * model change. They exist as one list precisely because those used to be written separately, and two
 * settings ended up with the hide but not the clear. So the shape of every capability's list is asserted
 * here rather than left to review.
 */
const declaring = (keys: string) => ({ "profileSettings.unsupported": keys });

const chat = (overrides: Partial<UaiChatProfileSettings> = {}): UaiChatProfileSettings => ({
    $type: "chat",
    temperature: 0.7,
    maxTokens: 500,
    systemPromptTemplate: "be nice",
    contextIds: [],
    guardrailIds: [],
    ...overrides,
});

describe("declared setting rules", () => {
    it("covers every capability, so none is silently left without rules", () => {
        expect(UAI_CHAT_SETTING_RULES.length).toBeGreaterThan(0);
        expect(UAI_EMBEDDING_SETTING_RULES.length).toBeGreaterThan(0);
        expect(UAI_SPEECH_TO_TEXT_SETTING_RULES.length).toBeGreaterThan(0);
        expect(UAI_IMAGE_GENERATION_SETTING_RULES.length).toBeGreaterThan(0);
    });

    it("uses the field keys the server declares", () => {
        expect(UAI_CHAT_SETTING_RULES.map((r) => r.key)).toContain(UAI_PROFILE_SETTING_KEYS.temperature);
        expect(UAI_EMBEDDING_SETTING_RULES.map((r) => r.key)).toContain(UAI_PROFILE_SETTING_KEYS.dimensions);
        expect(UAI_SPEECH_TO_TEXT_SETTING_RULES.map((r) => r.key)).toContain(UAI_PROFILE_SETTING_KEYS.language);
        expect(UAI_IMAGE_GENERATION_SETTING_RULES.map((r) => r.key)).toContain(UAI_PROFILE_SETTING_KEYS.mediaType);
    });
});

describe("isRuleSupported", () => {
    const [temperature] = UAI_CHAT_SETTING_RULES;

    it("renders a field the model says nothing about", () => {
        // Declarations are negative: silence is not a refusal.
        expect(isRuleSupported(temperature, undefined)).toBe(true);
        expect(isRuleSupported(temperature, declaring("dimensions"))).toBe(true);
    });

    it("hides a field the model declares unsupported", () => {
        expect(isRuleSupported(temperature, declaring("temperature"))).toBe(false);
        expect(isRuleSupported(temperature, declaring("temperature,topP,topK"))).toBe(false);
    });
});

describe("pruneDeclaredSettings", () => {
    it("clears a declared-unsupported value", () => {
        const pruned = pruneDeclaredSettings(chat(), declaring("temperature"), UAI_CHAT_SETTING_RULES);

        expect(pruned?.temperature).toBeNull();
    });

    it("leaves everything else alone", () => {
        const pruned = pruneDeclaredSettings(chat(), declaring("temperature"), UAI_CHAT_SETTING_RULES);

        expect(pruned?.maxTokens).toBe(500);
        expect(pruned?.systemPromptTemplate).toBe("be nice");
    });

    it("returns undefined when nothing needs changing, so the stored value is untouched", () => {
        expect(pruneDeclaredSettings(chat(), undefined, UAI_CHAT_SETTING_RULES)).toBeUndefined();
        expect(pruneDeclaredSettings(chat(), declaring("dimensions"), UAI_CHAT_SETTING_RULES)).toBeUndefined();
    });

    it("returns undefined when the field is already empty", () => {
        const unset = chat({ temperature: null });

        expect(pruneDeclaredSettings(unset, declaring("temperature"), UAI_CHAT_SETTING_RULES)).toBeUndefined();
    });

    it("returns undefined for missing settings", () => {
        expect(pruneDeclaredSettings(null, declaring("temperature"), UAI_CHAT_SETTING_RULES)).toBeUndefined();
    });

    it("clears embedding dimensions", () => {
        const settings: UaiEmbeddingProfileSettings = { $type: "embedding", dimensions: 256 };

        expect(pruneDeclaredSettings(settings, declaring("dimensions"), UAI_EMBEDDING_SETTING_RULES)?.dimensions)
            .toBeNull();
    });

    it("keeps a dimension count of zero out of the way of falsy checks", () => {
        // 0 is a real stored value, and a `!settings.dimensions` test would have skipped it.
        const settings: UaiEmbeddingProfileSettings = { $type: "embedding", dimensions: 0 };

        expect(pruneDeclaredSettings(settings, declaring("dimensions"), UAI_EMBEDDING_SETTING_RULES)?.dimensions)
            .toBeNull();
    });

    it("clears a speech-to-text language", () => {
        const settings: UaiSpeechToTextProfileSettings = { $type: "speechToText", language: "en" };

        expect(pruneDeclaredSettings(settings, declaring("language"), UAI_SPEECH_TO_TEXT_SETTING_RULES)?.language)
            .toBeNull();
    });

    it("clears an image media type but leaves the size, which is enumerated rather than declared", () => {
        const settings: UaiImageGenerationProfileSettings = {
            $type: "imageGeneration",
            size: "1024x1024",
            mediaType: "image/png",
        };

        const pruned = pruneDeclaredSettings(settings, declaring("mediaType"), UAI_IMAGE_GENERATION_SETTING_RULES);

        expect(pruned?.mediaType).toBeNull();
        expect(pruned?.size).toBe("1024x1024");
    });
});
