import { App, Card, Button, Form, Tabs, Modal, Input, Spin, Table, Typography, Space } from 'antd';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '@hooks';
import { TextInput, PasswordInput } from '@components/inputs';
import { useState, useEffect } from 'react';
import Api, { useExceptionNotification } from '@services/api';

const SignIn = () => {
  const navigate = useNavigate();
  const { message } = App.useApp();
  const { signIn, register } = useAuth();
  const [loading, setLoading] = useState(false);
  const [loginForm] = Form.useForm();
  const [regForm] = Form.useForm();
  const { showError } = useExceptionNotification();

  const [allUsersOpen, setAllUsersOpen] = useState(false);
  const [allUsersLoading, setAllUsersLoading] = useState(false);
  const [allUsers, setAllUsers] = useState([]);

  const [lookupOpen, setLookupOpen] = useState(false);
  const [lookupLogin, setLookupLogin] = useState('');
  const [lookupLoading, setLookupLoading] = useState(false);
  const [lookupUser, setLookupUser] = useState(null);



  const handleLogin = async (values) => {
    setLoading(true);
    try {
      const result = await signIn(values);
      if (result.success) navigate('/');
      else showError(result.error);
    } catch (error) {
      showError(error);
    } finally {
      setLoading(false);
    }
  };

  const handleRegister = async (values) => {
    setLoading(true);
    try {
      const result = await register(values);
      if (result.success) navigate('/');
      else showError(result.error);
    } catch (error) {
      showError(error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        minHeight: '100vh',
        background: 'linear-gradient(145deg, #0f172a 0%, #1e3a5f 50%, #0d9488 100%)',
        padding: 16,
      }}
    >
      <Card
        style={{
          width: 'min(420px, 100%)',
          boxShadow: '0 12px 40px rgba(0,0,0,0.25)',
          borderRadius: 12,
        }}
      >
        <Tabs
          items={[
            {
              key: 'login',
              label: 'Entrar',
              children: (
                <Form form={loginForm} layout="vertical" onFinish={handleLogin}>
                  <TextInput
                    name="username"
                    label="Usuário"
                    placeholder="Seu login"
                    rules={[{ required: true, message: 'Obrigatório' }]}
                  />
                  <PasswordInput
                    name="password"
                    label="Senha"
                    placeholder="Sua senha"
                    rules={[{ required: true, message: 'Obrigatório' }]}
                  />
                  <Button type="primary" block htmlType="submit" loading={loading} style={{ marginTop: 8 }}>
                    Entrar
                  </Button>
                </Form>
              ),
            },
            {
              key: 'reg',
              label: 'Criar conta',
              children: (
                <Form form={regForm} layout="vertical" onFinish={handleRegister}>
                  <TextInput
                    name="userName"
                    label="Usuário"
                    placeholder="Escolha um login"
                    rules={[{ required: true, message: 'Obrigatório' }]}
                  />
                  <TextInput
                    name="email"
                    label="E-mail"
                    placeholder="voce@email.com"
                    rules={[
                      { required: true, message: 'Obrigatório' },
                      { type: 'email', message: 'E-mail inválido' },
                    ]}
                  />
                  <PasswordInput
                    name="password"
                    label="Senha"
                    placeholder="Mín. 8 caracteres, maiúscula, minúscula, número e símbolo"
                    rules={[{ required: true, message: 'Obrigatório' }]}
                  />
                  <Button type="primary" block htmlType="submit" loading={loading} style={{ marginTop: 8 }}>
                    Registrar e entrar
                  </Button>
                </Form>
              ),
            },
          ]}
        />
      </Card>

    
    </div>
  );
};

export default SignIn;
