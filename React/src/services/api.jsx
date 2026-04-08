import axios from 'axios';
import { App } from 'antd';

const project = 'framework';

// VITE_API_URL vazio → mesma origem (localhost:3000) + proxy no vite.config.js → API HTTPS (IIS Express).
// Se definir, use HTTPS na porta do IIS Express, ex.: https://localhost:44363 (nunca http nessa porta).
const baseURL = import.meta.env.VITE_API_URL ?? '';

const Api = axios.create({
  baseURL,
  timeout: 30000,
});

// Interceptor de requisição - adiciona token automaticamente
Api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem(`${project}:token`);
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Hook para notificações de exceção
export const useExceptionNotification = () => {
  const { notification } = App.useApp();

  const showError = (error) => {
    if (error.response) {
      const { status, data } = error.response;

      switch (status) {
        case 401:
          // Não autorizado - limpar localStorage e redirecionar
          localStorage.removeItem(`${project}:token`);
          localStorage.removeItem(`${project}:user`);
          localStorage.removeItem(`${project}:userRoles`);
          localStorage.removeItem(`${project}:userName`);
          window.location.href = '/signIn';
          notification.error({
            message: 'Sessão Expirada',
            description: 'Sua sessão expirou. Por favor, faça login novamente.',
            duration: 3,
          });
          break;

        case 400: {
          const errList = Array.isArray(data?.errors) ? data.errors : [];
          const errorMessages =
            errList.length > 0
              ? errList
              : [data?.message || data?.Message || 'Erro de validação'];
          notification.error({
            message: 'Erro de Validação',
            description: errorMessages.join(', '),
            duration: 5,
          });
          break;
        }

        case 404:
          notification.error({
            message: 'Não Encontrado',
            description: 'O recurso solicitado não foi encontrado.',
            duration: 3,
          });
          break;

        case 500:
          notification.error({
            message: 'Erro do Servidor',
            description: 'Ocorreu um erro no servidor. Tente novamente mais tarde.',
            duration: 5,
          });
          break;

        default:
          notification.error({
            message: 'Erro',
            description: data?.message || 'Ocorreu um erro inesperado.',
            duration: 5,
          });
      }
    } else if (error.request) {
      notification.error({
        message: 'Erro de Conexão',
        description: 'Não foi possível conectar ao servidor. Verifique sua conexão.',
        duration: 5,
      });
    } else {
      notification.error({
        message: 'Erro',
        description: error.message || 'Ocorreu um erro inesperado.',
        duration: 5,
      });
    }
  };

  return { showError };
};

// Interceptor de resposta - tratamento de erros
Api.interceptors.response.use(
  (response) => response,
  (error) => {
    // O tratamento de erro será feito no componente que chama a API
    // usando o hook useExceptionNotification
    return Promise.reject(error);
  }
);

export default Api;
