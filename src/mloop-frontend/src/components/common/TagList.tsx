import { FC } from 'react';
import { SlTag } from '@shoelace-style/shoelace/dist/react';

interface TagListProps {
  tags: string[];
}

export const TagList: FC<TagListProps> = ({ tags }) => {
  return (
    <div className="flex flex-wrap gap-2">
      {tags.map((tag) => (
        <SlTag 
          key={tag}
          variant="neutral"
          size="small"
          className="text-sm"
        >
          {tag}
        </SlTag>
      ))}
    </div>
  );
};