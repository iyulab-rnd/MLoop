import React, { createContext, useContext, useCallback, useState } from 'react';
import { SlAlert } from '@shoelace-style/shoelace/dist/react';

export type NotificationType = 'success' | 'danger' | 'warning' | 'info';

interface Notification {
  id: string;
  type: NotificationType;
  message: string;
}

interface NotificationContextType {
  notifications: Notification[];
  showNotification: (type: NotificationType, message: string) => void;
  hideNotification: (id: string) => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export function NotificationProvider({ children }: { children: React.ReactNode }) {
  const [notifications, setNotifications] = useState<Notification[]>([]);

  const hideNotification = useCallback((id: string) => {
    setNotifications(prev => prev.filter(notification => notification.id !== id));
  }, []);

  const showNotification = useCallback((type: NotificationType, message: string) => {
    console.log(`Notification shown: [${type}] ${message}`);
    const id = Date.now().toString();
    setNotifications(prev => [...prev, { id, type, message }]);

    // 3초 후 자동으로 알림 제거
    setTimeout(() => {
      hideNotification(id);
    }, 3000);
  }, [hideNotification]);

  // NotificationType을 SlAlert의 variant 타입으로 매핑
  const mapVariant = (type: NotificationType): 'success' | 'danger' | 'warning' | 'primary' | 'neutral' => {
    switch(type) {
      case 'success':
      case 'danger':
      case 'warning':
        return type;
      case 'info':
        return 'neutral';
      default:
        return 'neutral';
    }
  };

  return (
    <NotificationContext.Provider value={{ notifications, showNotification, hideNotification }}>
      {children}
      {/* 알림을 고정된 위치에 렌더링 */}
      <div className="fixed bottom-4 right-4 z-100 space-y-2">
        {notifications.map(({ id, type, message }) => (
          <SlAlert
            key={id}
            open
            variant={mapVariant(type)}
            className="w-80"
            closable
            onSlAfterHide={() => hideNotification(id)}
          >
            {message}
          </SlAlert>
        ))}
      </div>
    </NotificationContext.Provider>
  );
}

export function useNotification() {
  const context = useContext(NotificationContext);
  if (context === undefined) {
    throw new Error('useNotification must be used within a NotificationProvider');
  }
  return context;
}