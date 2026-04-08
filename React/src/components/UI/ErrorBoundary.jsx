import { Component } from 'react';
import { Result, Button } from 'antd';
import { HomeOutlined, ReloadOutlined } from '@ant-design/icons';

/**
 * Error Boundary para capturar erros de renderização
 * Class Component necessário para Error Boundary
 */
class ErrorBoundary extends Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error) {
    return { hasError: true, error };
  }

  componentDidCatch(error, errorInfo) {
    console.error('ErrorBoundary capturou um erro:', error, errorInfo);
  }

  handleRetry = () => {
    this.setState({ hasError: false, error: null });
  };

  handleGoHome = () => {
    window.location.href = '/';
  };

  render() {
    if (this.state.hasError) {
      return (
        <Result
          status="500"
          title="500"
          subTitle="Desculpe, algo deu errado."
          extra={[
            <Button
              type="primary"
              key="retry"
              icon={<ReloadOutlined />}
              onClick={this.handleRetry}
            >
              Tentar Novamente
            </Button>,
            <Button
              key="home"
              icon={<HomeOutlined />}
              onClick={this.handleGoHome}
            >
              Voltar para Home
            </Button>,
          ]}
        />
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;
