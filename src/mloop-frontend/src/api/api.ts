import { apiRequest } from './client';

export const api = {
  get: async <T>(url: string, options?: RequestInit): Promise<T> => {
    return await apiRequest<T>(url, { method: 'GET', ...options });
  },

  post: async <T, U>(url: string, body: U, options?: RequestInit): Promise<T> => {
    let headers: Record<string, string> = {};
    let processedBody: string | FormData;
    
    if (body instanceof FormData) {
      headers = { ...Object(options?.headers) };
      processedBody = body;
    } else if (
      typeof body === 'string' && 
      options?.headers && 
      Object(options.headers)['Content-Type']?.startsWith('text/')
    ) {
      // Handle text/* content types (including TSV, CSV) - send as raw string
      headers = {
        'Content-Type': Object(options.headers)['Content-Type'],
        ...Object(options.headers)
      };
      processedBody = body;
    } else {
      // Default JSON handling
      headers = {
        'Content-Type': 'application/json',
        ...Object(options?.headers)
      };
      processedBody = JSON.stringify(body);
    }
  
    return await apiRequest<T>(url, {
      method: 'POST',
      headers,
      body: processedBody,
      ...options,
    });
  },

  put: async <T, U>(url: string, body: U, options?: RequestInit): Promise<T> => {
    return await apiRequest<T>(url, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        ...Object(options?.headers),
      },
      body: JSON.stringify(body),
      ...options,
    });
  },

  delete: async <T>(url: string, options?: RequestInit): Promise<T> => {
    return await apiRequest<T>(url, { method: 'DELETE', ...options });
  },
};