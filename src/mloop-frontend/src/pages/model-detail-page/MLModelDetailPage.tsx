import { useParams } from 'react-router-dom';
import { SlTabGroup, SlTab, SlTabPanel, SlAlert } from '@shoelace-style/shoelace/dist/react';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { ModelDetailHeader } from './components/ModelDetailHeader';
import { PerformanceMetrics } from './components/PerformanceMetrics';
import { TrainingArguments } from './components/TrainingArguments';
import { ModelLogs } from './components/ModelLogs';
import { useModelDetails } from './hooks/useModelDetails';

export const MLModelDetailPage = () => {
  const { scenarioId, modelId } = useParams();
  const {
    model,
    loading,
    error,
    logs,
    activeTab,
    setActiveTab,
    handleRefreshLogs,
  } = useModelDetails(scenarioId, modelId);

  if (loading) {
    return <LoadingSpinner />;
  }

  if (error || !model) {
    return (
      <div className="p-6">
        <SlAlert variant="danger">
          {error || "Model not found"}
        </SlAlert>
      </div>
    );
  }

  return (
    <div className="p-6">
      <ModelDetailHeader model={model} scenarioId={scenarioId!} />

      <SlTabGroup>
        <SlTab
          slot="nav"
          panel="details"
          active={activeTab === 'details'}
          onClick={() => setActiveTab('details')}
        >
          Details
        </SlTab>
        <SlTab
          slot="nav"
          panel="logs"
          active={activeTab === 'logs'}
          onClick={() => setActiveTab('logs')}
        >
          Logs
        </SlTab>

        <SlTabPanel name="details">
          <div className="space-y-8">
            <PerformanceMetrics metrics={model.metrics} />
            <TrainingArguments arguments={model.arguments} />
          </div>
        </SlTabPanel>

        <SlTabPanel name="logs">
          <ModelLogs logs={logs} onRefresh={handleRefreshLogs} />
        </SlTabPanel>
      </SlTabGroup>
    </div>
  );
};