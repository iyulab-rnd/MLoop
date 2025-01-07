import { FC } from 'react';
import { SlCard } from '@shoelace-style/shoelace/dist/react';
import { Scenario } from '../../../types';
import { TagList } from '../../../components/common/TagList';
import './ScenarioCard.css';

interface ScenarioCardProps {
  scenario: Scenario;
  onClick: (scenario: Scenario) => void;
}

export const ScenarioCard: FC<ScenarioCardProps> = ({ scenario, onClick }) => {
  return (
    <div className="scenario-card-wrapper">
      <SlCard 
        className="scenario-card"
        onClick={() => onClick(scenario)}
      >
        <div className="scenario-card-content">
          {/* Header - ML Type */}
          <div className="scenario-card-header">
            <span className="scenario-card-type">
              {scenario.mlType.replace('-', ' ')}
            </span>
          </div>

          {/* Title & Description Container */}
          <div className="scenario-card-body">
            <h2 className="scenario-card-title">
              {scenario.name}
            </h2>
            
            <p className="scenario-card-description">
              {scenario.description}
            </p>
          </div>

          {/* Tags Container */}
          <div className="scenario-card-tags">
            <TagList tags={scenario.tags} />
          </div>
          
          {/* Footer */}
          <div className="scenario-card-footer">
            <div className="scenario-card-date">
              Created: {new Date(scenario.createdAt).toLocaleDateString()}
            </div>
          </div>
        </div>
      </SlCard>
    </div>
  );
};