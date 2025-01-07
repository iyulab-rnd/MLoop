import { Model } from '../../../types/Model';
import { formatArgumentValue } from '../../../utils/formatters';

interface TrainingArgumentsProps {
  arguments: Model['arguments'];
}

export const TrainingArguments: React.FC<TrainingArgumentsProps> = ({
  arguments: args,
}) => {
  return (
    <div className="bg-white rounded-lg shadow p-6">
      <h3 className="text-lg font-medium mb-4">Training Arguments</h3>
      <div className="overflow-hidden">
        <table className="min-w-full">
          <thead className="bg-gray-50">
            <tr>
              <th
                scope="col"
                className="py-3.5 pl-4 pr-3 text-left text-sm font-semibold text-gray-900 sm:pl-6"
              >
                Argument
              </th>
              <th
                scope="col"
                className="px-3 py-3.5 text-left text-sm font-semibold text-gray-900"
              >
                Value
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 bg-white">
            {Object.entries(args)
              .sort()
              .map(([key, value]) => (
                <tr key={key}>
                  <td className="py-4 pl-4 pr-3 text-sm text-gray-600 sm:pl-6">
                    {key.replace(/-/g, ' ')}
                  </td>
                  <td className="px-3 py-4 text-sm font-medium text-gray-900 whitespace-pre-wrap break-all">
                    {formatArgumentValue(value)}
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
