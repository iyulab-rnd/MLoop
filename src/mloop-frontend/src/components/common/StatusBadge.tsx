interface StatusBadgeProps {
  status: string;
  variant?: "default" | "success" | "warning" | "danger" | "info";
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({
  status,
  variant = "default",
}) => {
  const getStatusColor = (variant: string) => {
    switch (variant) {
      case "success":
        return "bg-green-100 text-green-800";
      case "warning":
        return "bg-yellow-100 text-yellow-800";
      case "danger":
        return "bg-red-100 text-red-800";
      case "info":
        return "bg-blue-100 text-blue-800";
      default:
        return "bg-gray-100 text-gray-800";
    }
  };

  return (
    <span
      className={`px-2 py-1 text-xs font-medium rounded-md ${getStatusColor(
        variant
      )}`}
    >
      {status}
    </span>
  );
};
