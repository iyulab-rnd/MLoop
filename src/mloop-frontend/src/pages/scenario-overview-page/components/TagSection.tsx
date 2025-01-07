import { SlTag } from "@shoelace-style/shoelace/dist/react";

interface TagSectionProps {
  tags: string[];
}

export const TagSection: React.FC<TagSectionProps> = ({ tags }) => {
  return (
    <div className="mb-6">
      <h2 className="text-lg text-gray-500 mb-2">Tags</h2>
      <div className="flex flex-wrap gap-2">
        {tags.map((tag) => (
          <SlTag key={tag} variant="neutral" size="medium">
            {tag}
          </SlTag>
        ))}
      </div>
    </div>
  );
};
