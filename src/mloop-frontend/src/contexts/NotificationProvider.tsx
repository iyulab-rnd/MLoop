import React, { useCallback, useState } from 'react';
import { SlAlert } from '@shoelace-style/shoelace/dist/react';
import { NotificationType, Notification, mapVariant } from '../types/Notification';
import { NotificationContext } from './NotificationContext';

export function NotificationProvider({ children }: { children: React.ReactNode }) {
  const [notifications, setNotifications] = useState<Notification[]>([]);

  const hideNotification = useCallback((id: string) => {
    setNotifications(prev => prev.filter(notification => notification.id !== id));
  }, []);

  const showNotification = useCallback((type: NotificationType, message: string) => {
    console.log(`Notification shown: [${type}] ${message}`);
    const id = Date.now().toString();
    setNotifications(prev => [...prev, { id, type, message }]);
    setTimeout(() => {
      hideNotification(id);
    }, 3000);
  }, [hideNotification]);

  return (
    <NotificationContext.Provider value={{ notifications, showNotification, hideNotification }}>
      {children}
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
