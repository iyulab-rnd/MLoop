import { SlInput } from '@shoelace-style/shoelace/dist/react';
import { SlIcon } from '@shoelace-style/shoelace/dist/react';

interface ScenarioSearchProps {
  value: string;
  onChange: (value: string) => void;
}

export const ScenarioSearch = ({ value, onChange }: ScenarioSearchProps) => {
  const handleInput = (e: CustomEvent) => {
    const target = e.target as HTMLInputElement;
    onChange(target.value);
  };

  return (
    <div className="relative">
      <SlInput
        className="w-full max-w-md shadow-sm"
        size="large"
        type="search"
        placeholder="Search scenarios by name or tags..."
        value={value}
        onSlInput={handleInput}
        clearable
      >
        <SlIcon slot="prefix" name="search" />
      </SlInput>
    </div>
  );
};