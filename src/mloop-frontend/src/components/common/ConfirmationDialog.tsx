import { SlDialog, SlButton } from "@shoelace-style/shoelace/dist/react";

interface ConfirmationDialogProps {
  open: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  onConfirm: () => void;
  onCancel: () => void;
  loading?: boolean;
  variant?: "danger" | "primary";
}

export const ConfirmationDialog: React.FC<ConfirmationDialogProps> = ({
  open,
  title,
  message,
  confirmLabel = "Confirm",
  cancelLabel = "Cancel",
  onConfirm,
  onCancel,
  loading = false,
  variant = "primary",
}) => {
  return (
    <SlDialog label={title} open={open} onSlAfterHide={onCancel}>
      <div className="p-4">
        <p className="text-gray-700 mb-4">{message}</p>
        <div className="flex justify-end gap-2">
          <SlButton variant="default" onClick={onCancel} disabled={loading}>
            {cancelLabel}
          </SlButton>
          <SlButton variant={variant} loading={loading} onClick={onConfirm}>
            {confirmLabel}
          </SlButton>
        </div>
      </div>
    </SlDialog>
  );
};
