export class ApiError extends Error {
  public status: number;
  public data: any;

  constructor(message: string, status: number, data: any) {
    super(message);
    this.status = status;
    this.data = data;
    Object.setPrototypeOf(this, ApiError.prototype); // TypeScript에서 Error 클래스를 올바르게 확장하기 위해 필요
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
    let errorData: any = null;

    try {
      const contentType = response.headers.get("content-type");
      if (contentType && contentType.includes("application/json")) {
        // JSON 형식의 에러 응답 처리
        errorData = await response.json();
        errorMessage = errorData.message || errorMessage;
      } else if (contentType && contentType.includes("text/plain")) {
        // 텍스트 형식의 에러 응답 처리
        errorData = await response.text();
        errorMessage = errorData || errorMessage;
      } else {
        // 기타 형식의 에러 응답 처리
        errorData = await response.text();
        errorMessage = errorData || errorMessage;
      }
    } catch (e) {
      console.error("Error parsing error response:", e);
    }

    console.error(`API Error: ${errorMessage}`, errorData);
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
