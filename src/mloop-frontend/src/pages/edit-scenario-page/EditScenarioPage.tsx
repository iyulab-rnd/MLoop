import { useNavigate, useParams } from 'react-router-dom';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { ScenarioForm } from '../scenario-list-page/components/ScenarioForm';
import { EditScenarioHeader } from './components/EditScenarioHeader';
import { DeleteDialog } from './components/DeleteDialog';
import { useEditScenario } from './hooks/useEditScenario';

export const EditScenarioPage = () => {
  const { scenarioId } = useParams();
  const navigate = useNavigate();
  
  const {
    formData,
    setFormData,
    isSubmitting,
    isDeleting,
    showDeleteDialog,
    setShowDeleteDialog,
    error,
    loading,
    handleSubmit,
    handleDelete
  } = useEditScenario(scenarioId);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <LoadingSpinner />
      </div>
    );
  }

  return (
    <div className="max-w-[800px] mx-auto px-8 py-12">
      <EditScenarioHeader 
        onBack={() => navigate(`/scenarios/${scenarioId}`)}
        onDelete={() => setShowDeleteDialog(true)}
      />

      <ScenarioForm
        formData={formData}
        isSubmitting={isSubmitting}
        error={error}
        onSubmit={handleSubmit}
        onChange={setFormData}
        onCancel={() => navigate(`/scenarios/${scenarioId}`)}
        submitLabel="Save Changes"
      />

      <DeleteDialog
        open={showDeleteDialog}
        onClose={() => setShowDeleteDialog(false)}
        onConfirm={handleDelete}
        isDeleting={isDeleting}
      />
    </div>
  );
};