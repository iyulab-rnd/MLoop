import { SlAlert } from '@shoelace-style/shoelace/dist/react';

interface ContentWrapperProps {
  error: string | null;
  children: React.ReactNode;
}

export const ContentWrapper: React.FC<ContentWrapperProps> = ({
  error,
  children
}) => {
  if (error) {
    return (
      <div className="max-w-[800px] mx-auto px-8 py-12">
        <SlAlert variant="danger" className="text-center">
          {error}
        </SlAlert>
      </div>
    );
  }

  return (
    <div className="max-w-[1200px] mx-auto px-8 py-12">
      <div className="mb-8">
        {children}
      </div>
    </div>
  );
};
