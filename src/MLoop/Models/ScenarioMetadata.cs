﻿using System.ComponentModel.DataAnnotations;

namespace MLoop.Models;

public class ScenarioMetadata
{
    [Key]
    public string ScenarioId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string MLType { get; set; } = string.Empty;  // ML 작업 유형 (classification, regression 등)
    public DateTime CreatedAt { get; set; }
    public string? BestModelId { get; set; }
}