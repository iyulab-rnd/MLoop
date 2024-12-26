import { apiRequest } from './client';

// 제네릭을 사용하여 요청과 응답의 타입을 지정할 수 있습니다.
export const api = {
  get: async <T>(url: string, options?: RequestInit): Promise<T> => {
    return await apiRequest<T>(url, { method: 'GET', ...options });
  },

  post: async <T, U>(url: string, body: U, options?: RequestInit): Promise<T> => {
    let headers: Record<string, string> = {};
    
    // FormData인 경우 Content-Type 헤더를 설정하지 않음
    if (!(body instanceof FormData)) {
      headers = {
        'Content-Type': 'application/json',
        ...(options?.headers as Record<string, string>)
      };
    } else {
      headers = { ...(options?.headers as Record<string, string>) };
    }
  
    return await apiRequest<T>(url, {
      method: 'POST',
      headers,
      body: body instanceof FormData ? body : JSON.stringify(body),
      ...options,
    });
  },

  put: async <T, U>(url: string, body: U, options?: RequestInit): Promise<T> => {
    return await apiRequest<T>(url, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        ...(options && options.headers),
      },
      body: JSON.stringify(body),
      ...options,
    });
  },

  delete: async <T>(url: string, options?: RequestInit): Promise<T> => {
    return await apiRequest<T>(url, { method: 'DELETE', ...options });
  },
};
