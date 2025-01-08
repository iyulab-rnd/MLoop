import React, { useState } from 'react';
import { SlButton, SlIcon } from '@shoelace-style/shoelace/dist/react';

interface ImageUploadPanelProps {
  onUpload: (event: React.ChangeEvent<HTMLInputElement>) => void;
  predicting: boolean;
}

export const ImageUploadPanel: React.FC<ImageUploadPanelProps> = ({ onUpload, predicting }) => {
  const [uploadedImages, setUploadedImages] = useState<{ file: File; preview: string }[]>([]);

  const handleUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.files) {
      const newImages = Array.from(event.target.files).map(file => ({
        file,
        preview: URL.createObjectURL(file)
      }));
      setUploadedImages(prev => [...prev, ...newImages]);
    }
    onUpload(event);
  };

  const handleRemoveImage = (index: number) => {
    setUploadedImages(prev => {
      const newImages = [...prev];
      URL.revokeObjectURL(newImages[index].preview);
      newImages.splice(index, 1);
      return newImages;
    });
  };

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h2 className="text-lg font-medium">Upload Images</h2>
        <input
          type="file"
          id="imageUpload"
          className="hidden"
          accept="image/*"
          multiple
          onChange={handleUpload}
          disabled={predicting}
        />
        <SlButton
          variant="primary"
          onClick={() => document.getElementById('imageUpload')?.click()}
          loading={predicting}
        >
          <SlIcon slot="prefix" name="upload" />
          Upload Images
        </SlButton>
      </div>
      
      <div className="border-2 border-dashed rounded-lg h-[400px] bg-gray-50">
        {uploadedImages.length > 0 ? (
          <div className="h-full flex flex-col">
            <div className="flex justify-between items-center px-4 py-2 border-b bg-gray-100">
              <span className="text-sm font-medium truncate flex-1">
                {uploadedImages[0].file.name}
              </span>
              <button
                onClick={() => handleRemoveImage(0)}
                className="ml-2 text-gray-500 hover:text-red-500 transition-colors"
                disabled={predicting}
              >
                <SlIcon name="x" className="w-5 h-5" />
              </button>
            </div>
            <div className="flex-1 overflow-auto">
              <div className="h-full flex items-center justify-center p-4">
                <img
                  src={uploadedImages[0].preview}
                  alt={uploadedImages[0].file.name}
                  className="max-h-full max-w-full object-contain"
                />
              </div>
            </div>
          </div>
        ) : (
          <div 
            className="h-full flex items-center justify-center text-center cursor-pointer"
            onClick={() => document.getElementById('imageUpload')?.click()}
          >
            <div>
              <SlIcon name="images" className="w-16 h-16 text-gray-400 mb-4" />
              <p className="text-gray-600">
                Drag and drop images here or click to browse
              </p>
              <p className="text-sm text-gray-500 mt-2">
                Supports: JPG, PNG
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default ImageUploadPanel;