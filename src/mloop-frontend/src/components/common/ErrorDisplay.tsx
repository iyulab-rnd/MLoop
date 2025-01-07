import { FC } from 'react';

interface ErrorDisplayProps {
  message: string;
}

export const ErrorDisplay: FC<ErrorDisplayProps> = ({ message }) => {
  return (
    <div className="p-4 bg-red-50 rounded-lg text-red-600 text-center">
      Error loading scenarios: {message}
    </div>
  );
};