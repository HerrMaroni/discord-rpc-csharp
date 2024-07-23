using System;
using System.Text.Json.Serialization;

namespace DiscordRPC;

/// <summary>
///     Represents the structure for the Get Voice Settings response.
/// </summary>
[Serializable]
public class VoiceSettings
{
    /// <summary>
    ///     Input settings.
    /// </summary>
    [JsonPropertyName("input")]
    public VoiceSettingsInput Input { get; set; }

    /// <summary>
    ///     Output settings.
    /// </summary>
    [JsonPropertyName("output")]
    public VoiceSettingsOutput Output { get; set; }

    /// <summary>
    ///     Voice mode settings.
    /// </summary>
    [JsonPropertyName("mode")]
    public VoiceSettingsMode Mode { get; set; }

    /// <summary>
    ///     State of automatic gain control.
    /// </summary>
    [JsonPropertyName("automatic_gain_control")]
    public bool? AutomaticGainControl { get; set; }

    /// <summary>
    ///     State of echo cancellation.
    /// </summary>
    [JsonPropertyName("echo_cancellation")]
    public bool? EchoCancellation { get; set; }

    /// <summary>
    ///     State of noise suppression.
    /// </summary>
    [JsonPropertyName("noise_suppression")]
    public bool? NoiseSuppression { get; set; }

    /// <summary>
    ///     State of voice quality of service.
    /// </summary>
    [JsonPropertyName("qos")]
    public bool? Qos { get; set; }

    /// <summary>
    ///     State of silence warning notice.
    /// </summary>
    [JsonPropertyName("silence_warning")]
    public bool? SilenceWarning { get; set; }

    /// <summary>
    ///     State of self-deafen.
    /// </summary>
    [JsonPropertyName("deaf")]
    public bool? Deaf { get; set; }

    /// <summary>
    ///     State of self-mute.
    /// </summary>
    [JsonPropertyName("mute")]
    public bool? Mute { get; set; }
}

/// <summary>
///     Represents the input settings for voice.
/// </summary>
[Serializable]
public class VoiceSettingsInput
{
    /// <summary>
    ///     Device ID.
    /// </summary>
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; }

    /// <summary>
    ///     Input voice level (min: 0, max: 100).
    /// </summary>
    [JsonPropertyName("volume")]
    public float Volume { get; set; }

    /// <summary>
    ///     Array of read-only device objects containing id and name string keys.
    /// </summary>
    [JsonPropertyName("available_devices")]
    public DeviceObject[] AvailableDevices { get; set; }
}

/// <summary>
///     Represents the output settings for voice.
/// </summary>
[Serializable]
public class VoiceSettingsOutput
{
    /// <summary>
    ///     Device ID.
    /// </summary>
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; }

    /// <summary>
    ///     Output voice level (min: 0, max: 200).
    /// </summary>
    [JsonPropertyName("volume")]
    public float Volume { get; set; }

    /// <summary>
    ///     Array of read-only device objects containing id and name string keys.
    /// </summary>
    [JsonPropertyName("available_devices")]
    public DeviceObject[] AvailableDevices { get; set; }
}

/// <summary>
///     Represents the voice settings mode object for the Get Voice Settings response.
/// </summary>
[Serializable]
public class VoiceSettingsMode
{
    /// <summary>
    ///     Voice setting mode type (can be PUSH_TO_TALK or VOICE_ACTIVITY).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    ///     Voice activity threshold automatically sets its threshold.
    /// </summary>
    [JsonPropertyName("auto_threshold")]
    public bool AutoThreshold { get; set; }

    /// <summary>
    ///     Threshold for voice activity (in dB) (min: -100, max: 0).
    /// </summary>
    [JsonPropertyName("threshold")]
    public float Threshold { get; set; }

    /// <summary>
    ///     Shortcut key combos for PTT.
    /// </summary>
    [JsonPropertyName("shortcut")]
    public ShortcutKeyCombo[] Shortcut { get; set; }

    /// <summary>
    ///     The PTT release delay (in ms) (min: 0, max: 2000).
    /// </summary>
    [JsonPropertyName("delay")]
    public float Delay { get; set; }
}

/// <summary>
///     Represents a shortcut key combination for triggering actions like push-to-talk.
/// </summary>
[Serializable]
public class ShortcutKeyCombo
{
    /// <summary>
    ///     Type of the key (see key types).
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }

    /// <summary>
    ///     Key code of the shortcut.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    ///     Name of the key.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

/// <summary>
///     Represents a device object, containing information about an audio input or output device.
/// </summary>
[Serializable]
public class DeviceObject
{
    /// <summary>
    ///     Device identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    ///     Name of the device.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }
}