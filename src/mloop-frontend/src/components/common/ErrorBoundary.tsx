import React from 'react';
import { SlAlert, SlButton } from '@shoelace-style/shoelace/dist/react';

interface Props {
  children: React.ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends React.Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('ErrorBoundary caught an error:', error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="flex items-center justify-center min-h-screen p-4">
          <div className="max-w-md w-full">
            <SlAlert variant="danger" className="mb-4">
              <h3 className="text-lg font-semibold mb-2">Something went wrong</h3>
              <p className="text-sm mb-4">{this.state.error?.message}</p>
              <div className="flex justify-end">
                <SlButton 
                  variant="primary" 
                  size="small"
                  onClick={() => window.location.reload()}
                >
                  Reload Page
                </SlButton>
              </div>
            </SlAlert>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}