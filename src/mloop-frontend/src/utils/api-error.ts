export class ApiError extends Error {
  constructor(message: string, public status: number, public data?: unknown) {
    super(message);
    this.name = "ApiError";
    Object.setPrototypeOf(this, ApiError.prototype);
  }

  static isApiError(error: unknown): error is ApiError {
    return error instanceof ApiError;
  }
}

export function handleApiError(error: unknown): never {
  if (error instanceof ApiError) {
    throw error;
  }
  if (error instanceof Error) {
    throw new ApiError(error.message, 500);
  }
  throw new ApiError("An unexpected error occurred", 500);
}

// utils/api-helpers.ts
export async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const contentType = response.headers.get("content-type");
    let errorData: unknown = null;
    let message = `HTTP error! status: ${response.status}`;

    try {
      if (contentType?.includes("application/json")) {
        errorData = await response.json();
        message = (errorData as { message?: string })?.message || message;
      } else {
        errorData = await response.text();
      }
    } catch {
      // Ignore parsing errors
    }

    throw new ApiError(message, response.status, errorData);
  }

  const contentType = response.headers.get("content-type");
  if (contentType?.includes("application/json")) {
    return response.json();
  }
  return response.text() as unknown as T;
}
