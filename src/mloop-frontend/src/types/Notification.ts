export type NotificationType = 'success' | 'danger' | 'warning' | 'info';
export interface Notification {
  id: string;
  type: NotificationType;
  message: string;
}
export interface NotificationContextType {
  notifications: Notification[];
  showNotification: (type: NotificationType, message: string) => void;
  hideNotification: (id: string) => void;
}
export const mapVariant = (type: NotificationType): 'success' | 'danger' | 'warning' | 'primary' | 'neutral' => {
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