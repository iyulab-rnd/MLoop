// src/components/data/FileTable.tsx
import React, { useRef, useCallback, useState, useEffect } from "react";
import { useVirtualizer } from "@tanstack/react-virtual";
import ReactDOM from "react-dom"; // 포털 사용을 위해 유지
import {
  SlButton,
  SlIcon,
  SlMenu,
  SlMenuItem,
} from "@shoelace-style/shoelace/dist/react";
import { DataFile } from "../../types/DataFile";
import "./FileTable.css"; // CSS 파일 임포트
import { createPopper, Instance as PopperInstance } from "@popperjs/core"; // Popper.js 임포트

interface FileTableProps {
  files: DataFile[];
  onDelete: (path: string) => Promise<void>;
  onUnzip: (path: string) => Promise<void>;
  onView: (file: DataFile) => void;
  onNavigate: (path: string) => void;
  onDownload: (path: string, name: string) => void; // Download 핸들러 추가
}

// Utility function to hash string to number for consistent coloring
const hashCode = (str: string): number => {
  let hash = 0,
    i,
    chr;
  if (str.length === 0) return hash;
  for (i = 0; i < str.length; i++) {
    chr = str.charCodeAt(i);
    hash = (hash << 5) - hash + chr;
    hash |= 0; // Convert to 32bit integer
  }
  return hash;
};

// 파일 크기를 포맷하는 함수
const formatFileSize = (bytes: number): string => {
  const units = ["B", "KB", "MB", "GB"];
  let size = bytes;
  let unitIndex = 0;

  while (size >= 1024 && unitIndex < units.length - 1) {
    size /= 1024;
    unitIndex++;
  }

  return `${size.toFixed(1)} ${units[unitIndex]}`;
};

// 파일 리스트 아이템 컴포넌트
const FileRow = React.memo(
  ({
    file,
    style,
    onActionClick,
    onNavigate,
  }: {
    file: DataFile;
    style: React.CSSProperties;
    onActionClick: (
      event: React.MouseEvent<HTMLElement, MouseEvent>,
      file: DataFile
    ) => void;
    onNavigate: (path: string) => void;
  }) => {
    const canPreview =
      !file.isDirectory &&
      ["csv", "tsv", "txt"].includes(file.name.split(".").pop()?.toLowerCase() || "");
    const isZip =
      !file.isDirectory && (file.name.split(".").pop()?.toLowerCase() === "zip");

    return (
      <div
        className={`absolute top-0 left-0 w-full border-b border-gray-200 ${
          hashCode(file.path) % 2 === 0 ? "bg-white" : "bg-gray-50"
        }`}
        style={style}
      >
        <div className="grid grid-cols-[2fr,1fr,1.5fr,150px] gap-4 items-center py-2">
          {/* 파일 이름 및 아이콘 */}
          <div className="px-6">
            <div
              className={`flex items-center ${
                file.isDirectory ? "cursor-pointer hover:text-blue-600" : ""
              }`}
              onClick={() => (file.isDirectory ? onNavigate(file.path) : null)}
            >
              <SlIcon
                name={
                  file.isDirectory
                    ? "folder"
                    : isZip
                    ? "file-zip"
                    : "file-earmark"
                }
                className="mr-2"
              />
              <span className="truncate" title={file.name}>
                {file.name}
              </span>
            </div>
          </div>
          {/* 파일 크기 */}
          <div className="px-6">{formatFileSize(file.size)}</div>
          {/* 마지막 수정일 */}
          <div className="px-6">
            {new Date(file.lastModified).toLocaleString()}
          </div>
          {/* 액션 버튼 */}
          <div className="px-6 text-right relative">
            {/* 드롭다운 트리거 버튼 */}
            <SlButton
              variant="default"
              size="small"
              className="action-button"
              onClick={(e) => onActionClick(e, file)}
              // ref={file.ref} 제거
            >
              <SlIcon name="three-dots-vertical" />
            </SlButton>
          </div>
        </div>
      </div>
    );
  }
);

const FileTable: React.FC<FileTableProps> = ({
  files,
  onDelete,
  onUnzip,
  onView,
  onNavigate,
  onDownload,
}) => {
  const parentRef = useRef<HTMLDivElement>(null);

  const rowVirtualizer = useVirtualizer({
    count: files.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => 53,
    overscan: 5,
  });

  const measureElement = useCallback(
    (el: HTMLElement | null) => {
      if (el) {
        rowVirtualizer.measureElement(el);
      }
    },
    [rowVirtualizer]
  );

  // 드롭다운 상태 관리
  const [dropdownState, setDropdownState] = useState<{
    isOpen: boolean;
    file: DataFile | null;
    referenceElement: HTMLElement | null;
  }>({
    isOpen: false,
    file: null,
    referenceElement: null,
  });

  const popperInstanceRef = useRef<PopperInstance | null>(null);

  // 클릭 외부에서 드롭다운 닫기
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as HTMLElement;
      if (
        !target.closest(".action-button") &&
        !target.closest(".custom-global-dropdown")
      ) {
        setDropdownState({ isOpen: false, file: null, referenceElement: null });
      }
    };
    window.addEventListener("click", handleClickOutside);
    return () => window.removeEventListener("click", handleClickOutside);
  }, []);

  // 드롭다운 열기 핸들러
  const handleActionClick = (
    event: React.MouseEvent<HTMLElement, MouseEvent>,
    file: DataFile
  ) => {
    const reference = event.currentTarget as HTMLElement;

    setDropdownState({
      isOpen: true,
      file,
      referenceElement: reference,
    });
    event.stopPropagation(); // 외부 클릭 닫기 방지
  };

  // 드롭다운 닫기 핸들러
  const closeDropdown = () => {
    setDropdownState({ isOpen: false, file: null, referenceElement: null });
  };

  // Popper.js 인스턴스 생성 및 업데이트
  useEffect(() => {
    if (dropdownState.isOpen && dropdownState.referenceElement) {
      const dropdownElement = document.getElementById("dropdown-menu");
      if (dropdownElement) {
        popperInstanceRef.current = createPopper(
          dropdownState.referenceElement,
          dropdownElement,
          {
            placement: "bottom-start",
            modifiers: [
              {
                name: "offset",
                options: {
                  offset: [0, 8],
                },
              },
              {
                name: "preventOverflow",
                options: {
                  boundary: parentRef.current || "viewport",
                },
              },
              {
                name: "flip",
                options: {
                  fallbackPlacements: ["top-start"],
                },
              },
            ],
          }
        );
      }
    }

    return () => {
      if (popperInstanceRef.current) {
        popperInstanceRef.current.destroy();
        popperInstanceRef.current = null;
      }
    };
  }, [dropdownState.isOpen, dropdownState.referenceElement]);

  // 드롭다운을 포털로 렌더링
  const DropdownPortal = () => {
    if (!dropdownState.isOpen || !dropdownState.file) return null;

    return ReactDOM.createPortal(
      <div
        id="dropdown-menu"
        className={`custom-global-dropdown ${dropdownState.isOpen ? "open" : ""}`}
        role="menu"
        aria-label="File actions"
      >
        <SlMenu>
          {/* View option for previewable files */}
          {!dropdownState.file.isDirectory &&
            ["csv", "tsv", "txt"].includes(
              dropdownState.file.name.split(".").pop()?.toLowerCase() || ""
            ) && (
              <SlMenuItem
                onClick={() => {
                  onView(dropdownState.file!);
                  closeDropdown();
                }}
              >
                <SlIcon slot="prefix" name="eye" />
                View
              </SlMenuItem>
            )}

          {/* Unzip option for zip files */}
          {!dropdownState.file.isDirectory &&
            dropdownState.file.name.split(".").pop()?.toLowerCase() === "zip" && (
              <SlMenuItem
                onClick={() => {
                  onUnzip(dropdownState.file!.path);
                  closeDropdown();
                }}
              >
                <SlIcon slot="prefix" name="file-zip" />
                Unzip
              </SlMenuItem>
            )}

          {/* Download option for all files */}
          {!dropdownState.file.isDirectory && (
            <SlMenuItem
              onClick={() => {
                onDownload(dropdownState.file!.path, dropdownState.file!.name);
                closeDropdown();
              }}
            >
              <SlIcon slot="prefix" name="download" />
              Download
            </SlMenuItem>
          )}

          {/* Delete option for all files and directories */}
          <SlMenuItem
            onClick={() => {
              onDelete(dropdownState.file!.path);
              closeDropdown();
            }}
            className="text-red-600"
          >
            <SlIcon slot="prefix" name="trash" />
            Delete {dropdownState.file.isDirectory ? "Folder" : "File"}
          </SlMenuItem>
        </SlMenu>
      </div>,
      parentRef.current! // 포털의 대상: 스크롤 가능한 컨테이너 내부
    );
  };

  return (
    <div className="border border-gray-200 rounded-lg parent-container">
      {/* 테이블 헤더 */}
      <div className="bg-gray-50 border-b border-gray-200">
        <div className="grid grid-cols-[2fr,1fr,1.5fr,150px] gap-4">
          <div className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
            Name
          </div>
          <div className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
            Size
          </div>
          <div className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
            Last Modified
          </div>
          <div className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
            Actions
          </div>
        </div>
      </div>

      {/* 부모 컨테이너의 자식으로 스크롤 가능한 컨테이너 추가 */}
      <div className="relative" style={{ height: "calc(100vh - 400px)" }}>
        <div
          ref={parentRef}
          className="scrollable-content" // 스크롤 가능한 자식 컨테이너
        >
          <div
            style={{
              height: `${rowVirtualizer.getTotalSize()}px`,
              width: "100%",
              position: "relative",
            }}
          >
            {rowVirtualizer.getVirtualItems().map((virtualRow) => {
              const file = files[virtualRow.index];
              return (
                <FileRow
                  key={file.path}
                  file={file}
                  style={{
                    transform: `translateY(${virtualRow.start}px)`,
                  }}
                  onActionClick={handleActionClick}
                  onNavigate={onNavigate}
                />
              );
            })}
          </div>
        </div>
      </div>

      {/* 글로벌 드롭다운 포털 */}
      <DropdownPortal />
    </div>
  );
};

export default FileTable;
