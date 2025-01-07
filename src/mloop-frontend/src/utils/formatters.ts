export const formatMetricValue = (key: string, value: number): string => {
  if (
    key.toLowerCase().includes("score") ||
    key.toLowerCase().includes("accuracy")
  ) {
    return `${(value * 100).toFixed(2)}%`;
  }
  if (
    key.toLowerCase().includes("time") ||
    key.toLowerCase().includes("runtime")
  ) {
    return formatTime(value);
  }
  return Number.isInteger(value) ? value.toString() : value.toFixed(4);
};

export const formatArgumentValue = (
  value: string | number | boolean | null
): string => {
  if (value === null) return "null";
  return String(value);
};

export const formatTime = (value: number): string => {
  const hours = Math.floor(value / 3600);
  const minutes = Math.floor((value % 3600) / 60);
  const seconds = value % 60;

  const parts = [];
  if (hours > 0) parts.push(`${hours}h`);
  if (minutes > 0) parts.push(`${minutes}m`);
  if (seconds > 0 || parts.length === 0) parts.push(`${seconds.toFixed(1)}s`);

  return parts.join(" ");
};

export const formatFileSize = (bytes: number): string => {
  const units = ["B", "KB", "MB", "GB"];
  let size = bytes;
  let unitIndex = 0;

  while (size >= 1024 && unitIndex < units.length - 1) {
    size /= 1024;
    unitIndex++;
  }

  return `${size.toFixed(1)} ${units[unitIndex]}`;
};
