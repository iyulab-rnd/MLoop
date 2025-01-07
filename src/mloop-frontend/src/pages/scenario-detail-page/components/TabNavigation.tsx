import { Link } from 'react-router-dom';

interface TabItemProps {
  to: string;
  current: boolean;
  children: React.ReactNode;
}

const TabItem: React.FC<TabItemProps> = ({ to, current, children }) => (
  <Link
    to={to}
    className={`px-4 py-2 font-medium text-sm rounded-md transition-colors
      ${current 
        ? 'bg-white text-blue-600 shadow' 
        : 'text-gray-600 hover:text-gray-900 hover:bg-white/60'
      }`}
  >
    {children}
  </Link>
);

interface TabNavigationProps {
  currentPath: string;
  baseUrl: string;
}

export const TabNavigation: React.FC<TabNavigationProps> = ({
  currentPath,
  baseUrl,
}) => {
  return (
    <div className="flex gap-2 mt-6 bg-gray-100 p-1 rounded-md">
      <TabItem to={baseUrl} current={currentPath === baseUrl}>
        Overview
      </TabItem>
      <TabItem to={`${baseUrl}/models`} current={currentPath.includes('/models')}>
        Models
      </TabItem>
      <TabItem to={`${baseUrl}/data`} current={currentPath.includes('/data')}>
        Data
      </TabItem>
      <TabItem to={`${baseUrl}/workflows`} current={currentPath.includes('/workflows')}>
        Workflows
      </TabItem>
      <TabItem to={`${baseUrl}/predictions`} current={currentPath.includes('/predictions')}>
        Predictions
      </TabItem>
      <TabItem to={`${baseUrl}/jobs`} current={currentPath.includes('/jobs')}>
        Jobs
      </TabItem>
    </div>
  );
};
