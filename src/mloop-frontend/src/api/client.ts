export class ApiError extends Error {
  public status: number;
  public data: unknown;

  constructor(message: string, status: number, data: unknown) {
    super(message);
    this.status = status;
    this.data = data;
    Object.setPrototypeOf(this, ApiError.prototype);
  }
}

export async function apiRequest<T>(url: string, options?: RequestInit): Promise<T> {
  try {
    const response = await fetch(url, options);
    return await handleResponse<T>(response);
  } catch (error) {
    // 네트워크 오류 또는 기타 예외 처리
    if (error instanceof ApiError) {
      throw error;
    } else if (error instanceof Error) {
      throw new ApiError(error.message, 0, null);
    } else {
      throw new ApiError('An unknown error occurred.', 0, null);
    }
  }
}

async function handleResponse<T>(response: Response, parseResponse: boolean = true): Promise<T> {
  if (!response.ok) {
    let errorMessage = `HTTP error! status: ${response.status}`;
    let errorData: unknown = null;  // any를 unknown으로 변경

    try {
      const contentType = response.headers.get("content-type");
      if (contentType && contentType.includes("application/json")) {
        errorData = await response.json();
        errorMessage = (errorData as { message?: string })?.message || errorMessage;
      } else {
        errorData = await response.text();
      }
    } catch (e) {
      console.error("Error parsing error response:", e);
    }

    throw new ApiError(errorMessage, response.status, errorData);
  }

  // 성공 응답 처리
  if (parseResponse) {
    const contentType = response.headers.get("content-type");
    if (contentType && contentType.includes("application/json")) {
      return response.json() as Promise<T>; // Explicitly cast to T
    } else {
      return response.text() as unknown as T; // Ensure this aligns with expected T
    }
  }

  return null as unknown as T;
}
