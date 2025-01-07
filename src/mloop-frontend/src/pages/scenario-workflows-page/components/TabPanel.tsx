import { SlTab, SlTabGroup } from "@shoelace-style/shoelace/dist/react";

interface TabPanelProps {
  activeTab: string;
  onTabChange: (tab: string) => void;
  children: React.ReactNode;
}

export const TabPanel: React.FC<TabPanelProps> = ({
  activeTab,
  onTabChange,
  children,
}) => {
  return (
    <SlTabGroup>
      <SlTab
        slot="nav"
        panel="train"
        active={activeTab === "train"}
        onClick={() => onTabChange("train")}
      >
        Training Workflow
      </SlTab>
      <SlTab
        slot="nav"
        panel="predict"
        active={activeTab === "predict"}
        onClick={() => onTabChange("predict")}
      >
        Prediction Workflow
      </SlTab>
      {children}
    </SlTabGroup>
  );
};
