import { SlBreadcrumb, SlBreadcrumbItem } from "@shoelace-style/shoelace/dist/react";

interface DataBreadcrumbsProps {
  currentPath: string;
  onNavigate: (path: string) => void;
}

export const DataBreadcrumbs: React.FC<DataBreadcrumbsProps> = ({
  currentPath,
  onNavigate,
}) => {
  if (!currentPath) return null;

  const pathParts = currentPath.split("/");
  
  return (
    <SlBreadcrumb className="mb-4">
      <SlBreadcrumbItem onClick={() => onNavigate("")}>
        Root
      </SlBreadcrumbItem>
      {pathParts.map((part, index) => {
        const path = pathParts.slice(0, index + 1).join("/");
        return (
          <SlBreadcrumbItem
            key={path}
            onClick={() => onNavigate(path)}
          >
            {part}
          </SlBreadcrumbItem>
        );
      })}
    </SlBreadcrumb>
  );
};