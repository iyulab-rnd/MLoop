/**
 * Converts seconds into 'HH:MM:SS' or 'HHh MMm SSs' format
 * @param seconds Time in seconds
 * @param options Formatting options
 * @returns Formatted time string
 */
export const formatTime = (
  seconds: number,
  options: {
    showZero?: boolean; // Show units even when their value is 0
    useColon?: boolean; // Use '1:36:56' format instead of '1h 36m 56s'
  } = {}
): string => {
  const { showZero = false, useColon = false } = options;

  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const remainingSeconds = Math.round(seconds % 60);

  if (useColon) {
    // HH:MM:SS format
    if (hours > 0 || showZero) {
      return `${hours}:${minutes.toString().padStart(2, "0")}:${remainingSeconds
        .toString()
        .padStart(2, "0")}`;
    }
    return `${minutes}:${remainingSeconds.toString().padStart(2, "0")}`;
  }

  // HHh MMm SSs format
  const parts: string[] = [];

  if (hours > 0 || showZero) {
    parts.push(`${hours}h`);
  }

  if (minutes > 0 || showZero) {
    parts.push(`${minutes}m`);
  }

  if (remainingSeconds > 0 || showZero || parts.length === 0) {
    parts.push(`${remainingSeconds}s`);
  }

  return parts.join(" ");
};
