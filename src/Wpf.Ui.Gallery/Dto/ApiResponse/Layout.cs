// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json.Serialization;
using Wpf.Ui.Gallery.Converters;

namespace Wpf.Ui.Gallery.Dto;

public class Layout
{
    [JsonPropertyName("id")] public long Id { get; set; }

    [JsonPropertyName("clips")] public LayoutArea LayoutArea { get; set; }

    [JsonPropertyName("qr")] public QrCode QrCode { get; set; }

    [JsonPropertyName("patternPieceList")] public List<PatternPiecePosition> PatternPiecePositionList { get; set; }
}

public class LayoutArea
{
    [JsonPropertyName("height")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal HeightMm { get; set; }

    [JsonPropertyName("width")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal WidthMm { get; set; }
}

public class QrCode
{
    [JsonPropertyName("isAble")] public bool IsAble;

    [JsonPropertyName("height")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal Height { get; set; }

    [JsonPropertyName("width")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal Width { get; set; }
    
    [JsonPropertyName("offset_x")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal OffsetX { get; set; }

    [JsonPropertyName("offset_y")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal OffsetY { get; set; }

    [JsonPropertyName("rotate")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal Rotate { get; set; }

    [JsonPropertyName("url")] public string Url { get; set; }

    public string Content { get; set; }
}

public class PatternPiecePosition
{
    [JsonPropertyName("view_id")] public int ViewId { get; set; }
    
    [JsonPropertyName("patternPieceTitle")] public string PatternPieceTitle { get; set; }

    [JsonPropertyName("url")] public string Url { get; set; }

    [JsonPropertyName("rotate")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal Rotate { get; set; }

    [JsonPropertyName("offset_x")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal OffsetX { get; set; }

    [JsonPropertyName("offset_y")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal OffsetY { get; set; }

    [JsonPropertyName("tag")] public Tag Tag { get; set; }
}

public class Tag
{
    [JsonPropertyName("isAble")] public bool IsAble;

    [JsonPropertyName("height")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal Height { get; set; }

    [JsonPropertyName("width")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal Width { get; set; }

    [JsonPropertyName("rotate")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal Rotate { get; set; }

    [JsonPropertyName("offset_x")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal OffsetX { get; set; }

    [JsonPropertyName("offset_y")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal OffsetY { get; set; }
}