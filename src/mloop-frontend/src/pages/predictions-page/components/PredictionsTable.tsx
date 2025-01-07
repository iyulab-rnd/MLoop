import { useNavigate } from 'react-router-dom';
import { Prediction } from '../../../types/Prediction';

interface PredictionsTableProps {
  predictions: Prediction[];
  scenarioId: string;
}

export const PredictionsTable: React.FC<PredictionsTableProps> = ({
  predictions,
  scenarioId,
}) => {
  const navigate = useNavigate();

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case "waiting":
      case "processing":
        return "bg-blue-100 text-blue-800";
      case "completed":
        return "bg-green-100 text-green-800";
      case "failed":
        return "bg-red-100 text-red-800";
      default:
        return "bg-gray-100 text-gray-800";
    }
  };

  return (
    <div className="bg-white rounded-lg border border-gray-200">
      <table className="min-w-full divide-y divide-gray-200">
        <thead className="bg-gray-50">
          <tr>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Status
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Created At
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Model ID
            </th>
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-200">
          {predictions.map((prediction) => (
            <tr
              key={prediction.predictionId}
              className="hover:bg-gray-50 cursor-pointer"
              onClick={() => navigate(`/scenarios/${scenarioId}/predictions/${prediction.predictionId}`)}
            >
              <td className="px-6 py-4 whitespace-nowrap">
                <span
                  className={`px-2 py-1 text-xs font-medium rounded-md ${getStatusColor(
                    prediction.status
                  )}`}
                >
                  {prediction.status}
                </span>
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {new Date(prediction.createdAt).toLocaleString()}
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <div className="text-sm text-gray-900">
                  {prediction.modelId}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};