import React, { useState, useCallback, useRef, useEffect } from 'react';
import { SlInput, SlTag } from '@shoelace-style/shoelace/dist/react';
import type { SlInput as SlInputElement } from '@shoelace-style/shoelace';

interface TagInputProps {
  tags: string[];
  setTags: React.Dispatch<React.SetStateAction<string[]>>;
  placeholder?: string;
}

export const TagInput: React.FC<TagInputProps> = ({ tags, setTags, placeholder = "Type tag and press Enter..." }) => {
  const [inputValue, setInputValue] = useState('');
  const inputRef = useRef<SlInputElement>(null);

  // 태그 추가 핸들러
  const handleAddTag = useCallback((e?: React.KeyboardEvent<HTMLElement> | KeyboardEvent) => {
    if (e) {
      e.preventDefault();
      e.stopPropagation();
    }
    
    const newTag = inputValue.trim();
    if (newTag && !tags.includes(newTag)) {
      setTags([...tags, newTag]);
      setInputValue('');
    }
  }, [inputValue, tags, setTags]);

  // 태그 제거 핸들러
  const handleRemoveTag = useCallback((tagToRemove: string) => {
    setTags(tags.filter(tag => tag !== tagToRemove));
  }, [tags, setTags]);

  // SlInput 이벤트 핸들러
  const handleSlInput = useCallback((e: Event) => {
    const customEvent = e as CustomEvent<{ value: string }>;
    if (customEvent.detail && customEvent.detail.value !== undefined) {
      setInputValue(customEvent.detail.value);
    } else {
      const target = e.target as HTMLInputElement;
      setInputValue(target.value);
    }
  }, []);

  // SlInput onKeyDown 이벤트 핸들러
  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLElement>) => {
    if (e.key === 'Enter') {
      handleAddTag(e);
    }
  }, [handleAddTag]);

  // Shadow DOM 내 실제 input 요소에 keydown 이벤트 리스너 추가
  useEffect(() => {
    const slInput = inputRef.current;
    if (slInput) {
      const nativeInput = slInput.shadowRoot?.querySelector('input');
      if (nativeInput) {
        const onKeyDown = (e: KeyboardEvent) => {
          if (e.key === 'Enter') {
            handleAddTag(e);
          }
        };
        nativeInput.addEventListener('keydown', onKeyDown);
        return () => {
          nativeInput.removeEventListener('keydown', onKeyDown);
        };
      }
    }
  }, [handleAddTag]);

  return (
    <div onSubmit={e => e.preventDefault()}>
      <SlInput
        ref={inputRef}
        className="w-full mb-2"
        value={inputValue}
        onSlInput={handleSlInput}
        onKeyDown={handleKeyDown}
        placeholder={placeholder}
        clearable
      />
      <div className="flex flex-wrap gap-2">
        {tags.map(tag => (
          <SlTag 
            key={tag}
            className="cursor-pointer"
            removable
            onSlRemove={() => handleRemoveTag(tag)}
          >
            {tag}
          </SlTag>
        ))}
      </div>
    </div>
  );
};