import { SlButton, SlDialog } from '@shoelace-style/shoelace/dist/react';

interface DeleteDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
  isDeleting: boolean;
}

export const DeleteDialog: React.FC<DeleteDialogProps> = ({
  open,
  onClose,
  onConfirm,
  isDeleting
}) => {
  return (
    <SlDialog 
      label="Confirm Delete"
      open={open}
      onSlAfterHide={onClose}
    >
      <div className="p-4">
        <p className="text-gray-700 mb-4">
          Are you sure you want to delete this scenario? This action cannot be undone.
        </p>
        <div className="flex justify-end gap-2">
          <SlButton 
            variant="default" 
            onClick={onClose}
          >
            Cancel
          </SlButton>
          <SlButton 
            variant="danger" 
            loading={isDeleting}
            onClick={onConfirm}
          >
            Delete
          </SlButton>
        </div>
      </div>
    </SlDialog>
  );
};