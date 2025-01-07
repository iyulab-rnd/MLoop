import { SlButton, SlIcon } from '@shoelace-style/shoelace/dist/react';

interface ImageUploadPanelProps {
  onUpload: (event: React.ChangeEvent<HTMLInputElement>) => void;
  predicting: boolean;
}

export const ImageUploadPanel: React.FC<ImageUploadPanelProps> = ({ onUpload, predicting }) => {
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
          onChange={onUpload}
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
      <div 
        className="border-2 border-dashed rounded-lg h-[600px] p-4 bg-gray-50 flex items-center justify-center text-center cursor-pointer"
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
    </div>
  );
};