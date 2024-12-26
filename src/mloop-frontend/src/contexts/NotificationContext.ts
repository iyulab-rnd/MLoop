import { createContext } from 'react';
import { NotificationContextType } from '../types/Notification';

export const NotificationContext = createContext<NotificationContextType | undefined>(undefined);