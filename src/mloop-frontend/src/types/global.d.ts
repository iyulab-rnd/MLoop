// global.d.ts
declare namespace JSX {
    interface IntrinsicElements {
      'sl-select': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & {
        onSlChange?: (event: CustomEvent<{ value: string }>) => void;
      };
      'sl-menu-item': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement>;
      'sl-button': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement>;
      'sl-icon': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement>;
      'sl-alert': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement>;
      'sl-tab-group': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement>;
      'sl-tab': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement>;
      'sl-tab-panel': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement>;
      // 필요한 다른 Shoelace 컴포넌트들도 추가
    }
  }
  
  interface HTMLElementTagNameMap {
    'sl-select': HTMLElement;
    'sl-menu-item': HTMLElement;
    'sl-button': HTMLElement;
    'sl-icon': HTMLElement;
    'sl-alert': HTMLElement;
    'sl-tab-group': HTMLElement;
    'sl-tab': HTMLElement;
    'sl-tab-panel': HTMLElement;
    // 필요한 다른 Shoelace 컴포넌트들도 추가
  }
  